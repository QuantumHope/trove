namespace Trove.Web.YouTube;

/// <summary>The <c>YouTube</c> settings section. The key lives in user secrets or the environment, never in git.</summary>
public sealed class YouTubeOptions
{
    public const string Section = "YouTube";

    /// <summary>A YouTube Data API v3 key (<c>YouTube:ApiKey</c>, or <c>YouTube__ApiKey</c> in the environment).</summary>
    public string? ApiKey { get; set; }

    /// <summary>The channels to read, by handle (<c>@MKIceAndFire</c>).</summary>
    public List<string> Channels { get; set; } = [];

    /// <summary>How often to look for new uploads in the background.</summary>
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromHours(6);

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
