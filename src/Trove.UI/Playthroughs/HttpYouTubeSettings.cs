using System.Net.Http.Json;
using Trove.Core.Settings;

namespace Trove.UI.Playthroughs;

/// <summary>The Settings screen's YouTube section through the server's API.</summary>
public sealed class HttpYouTubeSettings(HttpClient http) : IYouTubeSettings
{
    private const string Path = "api/settings/youtube";

    public async Task<YouTubeSettingsView> GetAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<YouTubeSettingsView>(Path, cancellationToken)
        ?? throw new InvalidOperationException("The server sent no settings.");

    public async Task<SaveYouTubeKeyResult> SaveKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        using var response = await http.PutAsJsonAsync($"{Path}/key", new SaveYouTubeKeyRequest(apiKey), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SaveYouTubeKeyResult>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no answer.");
    }

    public async Task<YouTubeSettingsView> RemoveKeyAsync(CancellationToken cancellationToken = default)
    {
        using var response = await http.DeleteAsync($"{Path}/key", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<YouTubeSettingsView>(cancellationToken)
            ?? throw new InvalidOperationException("The server sent no settings.");
    }
}
