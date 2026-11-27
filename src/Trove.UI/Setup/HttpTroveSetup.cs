using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Trove.Core.Setup;

namespace Trove.UI.Setup;

/// <summary>CORA's <c>/trove/api/setup</c> routes.</summary>
public sealed class HttpTroveSetup(HttpClient http) : ITroveSetup
{
    public const string NotAdmin =
        "Only CORA's admin can set this. They can set it in CORA under Admin → Modules → Trove.";

    /// <summary>What to say when the server answered with something Trove can't read.</summary>
    public const string Unreadable = "Trove got an answer from the server it couldn't read. Try again in a moment.";

    public event Action? Changed;

    /// <summary>
    /// Whether <paramref name="exception"/> is the server being unreachable or answering with something unreadable,
    /// which the setup screens report rather than let take the app down.
    /// </summary>
    public static bool IsServerFailure(Exception exception) =>
        exception is HttpRequestException or JsonException or NotSupportedException or InvalidOperationException;

    /// <summary>The words to show for a <see cref="IsServerFailure"/> failure.</summary>
    public static string Describe(Exception exception) =>
        exception is HttpRequestException ? exception.Message : Unreadable;

    public async Task<SetupStatus> GetAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<SetupStatus>("api/setup", cancellationToken)
        ?? throw new InvalidOperationException("The server sent no setup.");

    public async Task<SaveCoraAddressResult> SaveCoraAddressAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        using var response = await http.PutAsJsonAsync("api/setup/cora-address", new { baseUrl }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new UnauthorizedAccessException(NotAdmin);
        }

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SaveCoraAddressResult>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no answer.");
        if (result.Saved)
        {
            Changed?.Invoke();
        }

        return result;
    }
}
