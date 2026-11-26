using Microsoft.Extensions.Options;
using Trove.Core.Settings;

namespace Trove.Web.YouTube;

/// <summary>The Settings screen's YouTube section, on the server.</summary>
public sealed class YouTubeSettingsService(
    YouTubeKeyStore keys, YouTubeDataApi api, YouTubeSync sync, IOptionsMonitor<YouTubeOptions> options,
    ILogger<YouTubeSettingsService> logger) : IYouTubeSettings
{
    public async Task<YouTubeSettingsView> GetAsync(CancellationToken cancellationToken = default)
    {
        var (key, source) = await keys.GetAsync(cancellationToken);
        return new YouTubeSettingsView(source, IYouTubeSettings.Ending(key), options.CurrentValue.HasApiKey, options.CurrentValue.Channels);
    }

    public async Task<SaveYouTubeKeyResult> SaveKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var key = apiKey?.Trim() ?? "";
        if (key.Length == 0)
        {
            return new SaveYouTubeKeyResult(false, "Paste a key first.", await GetAsync(cancellationToken));
        }

        // Try it before keeping it: one cheap call (1 unit of quota) that YouTube refuses if the key is wrong.
        try
        {
            await api.FindChannelAsync(options.CurrentValue.Channels.FirstOrDefault() ?? "@YouTube", cancellationToken, key);
        }
        catch (YouTubeApiException ex)
        {
            return new SaveYouTubeKeyResult(false, ex.Message, await GetAsync(cancellationToken));
        }
        catch (HttpRequestException)
        {
            return new SaveYouTubeKeyResult(false, "Trove couldn't reach YouTube to check the key. Try again in a moment.", await GetAsync(cancellationToken));
        }

        await keys.SaveAsync(key, cancellationToken);
        StartSync();
        return new SaveYouTubeKeyResult(true, null, await GetAsync(cancellationToken));
    }

    public async Task<YouTubeSettingsView> RemoveKeyAsync(CancellationToken cancellationToken = default)
    {
        await keys.RemoveAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }

    // A new key should fill the board without waiting for the next timed sync.
    private void StartSync() => _ = Task.Run(async () =>
    {
        try
        {
            await sync.SyncAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "YouTube sync after saving a key failed");
        }
    });
}
