using System.Net.Http.Json;
using System.Text.Json;
using Trove.Core.Viewing;

namespace Trove.UI.Viewing;

/// <summary>
/// CORA's <c>/web/frame-check</c>. It's one of CORA's own routes rather than the Trove module's, so the path is
/// root-relative: Trove lives at <c>/trove/</c> on the same address.
/// </summary>
public sealed class HttpFrameChecker(HttpClient http) : IFrameChecker
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<FrameCheck> CheckAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await http.GetAsync("/web/frame-check?url=" + Uri.EscapeDataString(url), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string? detail = null;
                try
                {
                    using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                    detail = CoraProblems.Detail(body.RootElement);
                }
                catch (JsonException)
                {
                }

                return new FrameCheck(url, Framing.Unknown, detail ?? $"CORA couldn't check the page ({(int)response.StatusCode}).");
            }

            return await response.Content.ReadFromJsonAsync<FrameCheck>(Json, cancellationToken)
                   ?? new FrameCheck(url, Framing.Unknown, "CORA sent no answer.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            return new FrameCheck(url, Framing.Unknown, ex.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient's own timeout.
            return new FrameCheck(url, Framing.Unknown, "CORA took too long to check the page.");
        }
    }
}
