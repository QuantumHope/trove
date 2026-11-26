using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trove.Core.Playthroughs;
using Trove.Web.Data;

namespace Trove.Web.YouTube;

/// <summary>
/// Reads each configured channel's uploads and adds the full-game videos to the board as New. The first sync pages
/// through the whole channel; later ones stop at the newest upload already seen. Videos already on the board keep
/// their state, rating and date.
/// </summary>
public sealed class YouTubeSync(
    IServiceScopeFactory scopes, IOptionsMonitor<YouTubeOptions> options, YouTubeKeyStore keys, TimeProvider time, ILogger<YouTubeSync> logger)
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public string? LastError { get; private set; }

    public async Task<SyncStatus> SyncAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            LastError = null;
            if (!await keys.HasKeyAsync(cancellationToken))
            {
                LastError = "No YouTube API key is set yet.";
            }
            else
            {
                foreach (var handle in options.CurrentValue.Channels)
                {
                    try
                    {
                        // A scope per channel, so a channel that fails halfway saves nothing.
                        using var channelScope = scopes.CreateScope();
                        await SyncChannelAsync(
                            channelScope.ServiceProvider.GetRequiredService<TroveDbContext>(),
                            channelScope.ServiceProvider.GetRequiredService<YouTubeDataApi>(),
                            handle,
                            cancellationToken);
                    }
                    catch (Exception ex) when (ex is YouTubeApiException or HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
                    {
                        logger.LogWarning("YouTube sync of {Handle} failed: {Reason}", handle, ex.Message);
                        LastError = $"{handle}: {ex.Message}";
                    }
                }
            }

            using var scope = scopes.CreateScope();
            return await GetStatusAsync(scope.ServiceProvider.GetRequiredService<TroveDbContext>(), cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<SyncStatus> GetStatusAsync(TroveDbContext db, CancellationToken cancellationToken)
    {
        var handles = options.CurrentValue.Channels;
        var synced = await db.YouTubeChannels.AsNoTracking().ToListAsync(cancellationToken);
        var lastSynced = handles.Count > 0 && handles.All(h => synced.Any(c => SameHandle(c.Handle, h) && c.LastSyncedAt is not null))
            ? synced.Where(c => handles.Any(h => SameHandle(c.Handle, h))).Min(c => c.LastSyncedAt)
            : null;
        return new SyncStatus(await keys.HasKeyAsync(cancellationToken), handles, lastSynced, LastError);
    }

    private async Task SyncChannelAsync(TroveDbContext db, YouTubeDataApi api, string handle, CancellationToken cancellationToken)
    {
        var channel = await db.YouTubeChannels.SingleOrDefaultAsync(c => c.Handle == handle, cancellationToken);
        if (channel is null)
        {
            var found = await api.FindChannelAsync(handle, cancellationToken)
                ?? throw new YouTubeApiException("YouTube has no channel with that handle.");
            channel = await db.YouTubeChannels.FindAsync([found.ChannelId], cancellationToken);
            if (channel is null)
            {
                channel = new YouTubeChannelRow
                {
                    ChannelId = found.ChannelId, Handle = handle, Title = found.Title, UploadsPlaylistId = found.UploadsPlaylistId,
                };
                db.YouTubeChannels.Add(channel);
            }
            else
            {
                channel.Handle = handle;
            }
        }

        var stopAt = channel.NewestUploadSeen;
        var newest = stopAt;
        var candidates = new List<string>();
        string? pageToken = null;
        do
        {
            var page = await api.GetUploadsAsync(channel.UploadsPlaylistId, pageToken, cancellationToken);
            var reachedSeen = false;
            foreach (var upload in page.Uploads)
            {
                if (upload.PublishedAt is { } published)
                {
                    if (stopAt is not null && published <= stopAt)
                    {
                        reachedSeen = true;
                        continue;
                    }

                    newest = newest is null || published > newest ? published : newest;
                }

                if (FullGameFilter.IsFullGame(upload.Title))
                {
                    candidates.Add(upload.VideoId);
                }
            }

            pageToken = reachedSeen ? null : page.NextPageToken;
        }
        while (pageToken is not null);

        var known = await db.YouTubeVideos.Where(v => candidates.Contains(v.VideoId)).Select(v => v.VideoId).ToListAsync(cancellationToken);
        var fresh = candidates.Except(known).Distinct().ToList();
        var now = time.GetUtcNow();
        foreach (var batch in fresh.Chunk(50))
        {
            foreach (var video in await api.GetVideosAsync(batch, cancellationToken))
            {
                // The video's own title is the current one; check it again in case it changed since it was listed.
                if (!FullGameFilter.IsFullGame(video.Title))
                {
                    continue;
                }

                db.YouTubeVideos.Add(new YouTubeVideoRow
                {
                    VideoId = video.VideoId,
                    ChannelId = channel.ChannelId,
                    ChannelTitle = video.ChannelTitle,
                    Title = video.Title,
                    PublishedAt = video.PublishedAt,
                    DurationSeconds = (int)video.Duration.TotalSeconds,
                    ThumbnailUrl = video.ThumbnailUrl,
                    ViewCount = video.ViewCount,
                    State = WatchState.New,
                    AddedAt = now,
                });
            }
        }

        channel.NewestUploadSeen = newest;
        channel.LastSyncedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("YouTube sync of {Handle} added {Count} full-game videos", handle, fresh.Count);
    }

    private static bool SameHandle(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Syncs once at startup and then every <see cref="YouTubeOptions.SyncInterval"/>, when a key is set.</summary>
public sealed class YouTubeSyncWorker(
    YouTubeSync sync, YouTubeKeyStore keys, IOptionsMonitor<YouTubeOptions> options, TimeProvider time, ILogger<YouTubeSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await keys.HasKeyAsync(stoppingToken))
                {
                    await sync.SyncAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // A database that isn't up yet shouldn't take the app down; try again next time.
                logger.LogError(ex, "Background YouTube sync failed");
            }

            await Task.Delay(options.CurrentValue.SyncInterval, time, stoppingToken);
        }
    }
}
