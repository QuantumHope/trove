namespace Trove.Core.Playthroughs;

/// <summary>A full-game video on the board, with what YouTube says about it and where you are with it.</summary>
public sealed record Playthrough(
    string VideoId,
    string Title,
    string ChannelTitle,
    DateTimeOffset PublishedAt,
    TimeSpan Duration,
    string ThumbnailUrl,
    long? ViewCount,
    WatchState State,
    decimal? Rating,
    DateOnly? WatchedOn)
{
    public string WatchUrl => YouTubeLinks.Watch(VideoId);
}

public static class YouTubeLinks
{
    public static string Watch(string videoId) => $"https://www.youtube.com/watch?v={Uri.EscapeDataString(videoId)}";
}

/// <summary>The board's videos and how the last sync went.</summary>
public sealed record PlaythroughBoard(IReadOnlyList<Playthrough> Videos, SyncStatus Sync);

/// <param name="HasApiKey">Whether a YouTube Data API key is set on the server.</param>
/// <param name="Channels">The channels Trove reads, as configured (handles like @MKIceAndFire).</param>
/// <param name="LastSyncedAt">When every channel last synced, if they have.</param>
/// <param name="LastError">Why the last sync failed, in plain words, if it did.</param>
public sealed record SyncStatus(bool HasApiKey, IReadOnlyList<string> Channels, DateTimeOffset? LastSyncedAt, string? LastError);

/// <summary>A move to another column, with the rating given at the same time (the prompt when moving to Done).</summary>
public sealed record MoveRequest(WatchState State, decimal? Rating);

public sealed record RateRequest(decimal? Rating);
