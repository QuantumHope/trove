using System.Net;
using System.Net.Http.Json;
using Trove.Core.Playthroughs;

namespace Trove.UI.Playthroughs;

/// <summary>The board through the server's API, for the browser (and later the phone shell).</summary>
public sealed class HttpPlaythroughLibrary(HttpClient http) : IPlaythroughLibrary
{
    public async Task<PlaythroughBoard> GetBoardAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<PlaythroughBoard>("api/playthroughs", cancellationToken)
        ?? throw new InvalidOperationException("The server sent no board.");

    public Task<Playthrough> MoveAsync(string videoId, MoveRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/playthroughs/{Uri.EscapeDataString(videoId)}/move", request, cancellationToken);

    public Task<Playthrough> RateAsync(string videoId, decimal? rating, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/playthroughs/{Uri.EscapeDataString(videoId)}/rating", new RateRequest(rating), cancellationToken);

    public async Task<SyncStatus> SyncAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsync("api/playthroughs/sync", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SyncStatus>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no sync status.");
    }

    private async Task<Playthrough> SendAsync<T>(HttpMethod method, string path, T body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("That video isn't on the board any more.");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException("Trove couldn't save that change.");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Playthrough>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no video.");
    }
}
