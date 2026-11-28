using System.Net.Http.Json;
using System.Text.Json;

namespace Trove.Desktop.Services;

/// <summary>What a check of a CORA address found.</summary>
public enum ProbeResult
{
    /// <summary>CORA answered and serves Trove.</summary>
    Ready,

    /// <summary>Something answered, but not with Trove (the module is off, or it isn't CORA).</summary>
    NoTrove,

    /// <summary>Nothing answered in time.</summary>
    Unreachable,
}

public interface ICoraProbe
{
    Task<ProbeResult> CheckAsync(string origin, CancellationToken cancellationToken = default);
}

/// <summary>
/// Asks for Trove's web app manifest, which CORA serves without sign-in. It answers quickly and says "Trove" by name,
/// so a wrong address that happens to be a web server isn't taken for CORA.
/// </summary>
public sealed class CoraProbe(HttpClient http) : ICoraProbe
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);

    public async Task<ProbeResult> CheckAsync(string origin, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            using var response = await http.GetAsync(new Uri(CoraAddress.TrovePage(origin), "manifest.webmanifest"), timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                return ProbeResult.NoTrove;
            }

            var manifest = await response.Content.ReadFromJsonAsync<JsonElement>(timeout.Token);
            return manifest.ValueKind == JsonValueKind.Object
                   && manifest.TryGetProperty("name", out var name) && name.GetString() == "Trove"
                ? ProbeResult.Ready
                : ProbeResult.NoTrove;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return ProbeResult.NoTrove;
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return ProbeResult.Unreachable;
        }
    }
}
