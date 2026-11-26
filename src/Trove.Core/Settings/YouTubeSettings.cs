using System.Text.Json.Serialization;

namespace Trove.Core.Settings;

/// <summary>Where the YouTube API key in use comes from.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<YouTubeKeySource>))]
public enum YouTubeKeySource
{
    /// <summary>No key anywhere yet.</summary>
    None,

    /// <summary>User secrets or the environment (<c>YouTube:ApiKey</c>).</summary>
    Configuration,

    /// <summary>Entered on the Settings screen and kept in Trove's database. Wins over configuration.</summary>
    Saved,
}

/// <summary>What the Settings screen shows. The key itself never leaves the server; only its last four characters do.</summary>
public sealed record YouTubeSettingsView(
    YouTubeKeySource Source, string? KeyEnding, bool ConfigurationHasKey, IReadOnlyList<string> Channels);

public sealed record SaveYouTubeKeyRequest(string ApiKey);

/// <summary>The outcome of saving a key: saved after a test call to YouTube worked, or the reason it didn't.</summary>
public sealed record SaveYouTubeKeyResult(bool Saved, string? Error, YouTubeSettingsView Settings);

public interface IYouTubeSettings
{
    Task<YouTubeSettingsView> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Tries the key against YouTube, and keeps it only if YouTube accepts it.</summary>
    Task<SaveYouTubeKeyResult> SaveKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>Forgets the saved key, falling back to configuration if it has one.</summary>
    Task<YouTubeSettingsView> RemoveKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>The last four characters of a key, for showing which one is in use.</summary>
    public static string? Ending(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : key.Trim()[^Math.Min(4, key.Trim().Length)..];
}
