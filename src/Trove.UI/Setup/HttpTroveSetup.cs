using System.Net;
using System.Net.Http.Json;
using Trove.Core.Setup;

namespace Trove.UI.Setup;

/// <summary>CORA's <c>/trove/api/setup</c> routes.</summary>
public sealed class HttpTroveSetup(HttpClient http) : ITroveSetup
{
    public const string NotAdmin =
        "Only CORA's admin can set this. They can set it in CORA under Admin → Modules → Trove.";

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
        return await response.Content.ReadFromJsonAsync<SaveCoraAddressResult>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no answer.");
    }
}
