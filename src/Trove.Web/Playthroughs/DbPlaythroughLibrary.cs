using Microsoft.EntityFrameworkCore;
using Trove.Core.Playthroughs;
using Trove.Web.Data;
using Trove.Web.YouTube;

namespace Trove.Web.Playthroughs;

/// <summary>The board straight from the database, for the server's API and for prerendering.</summary>
public sealed class DbPlaythroughLibrary(TroveDbContext db, YouTubeSync sync, TimeProvider time) : IPlaythroughLibrary
{
    public async Task<PlaythroughBoard> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        // SQLite can't order by DateTimeOffset, so sort in memory; one person's board is small.
        var rows = await db.YouTubeVideos.AsNoTracking().ToListAsync(cancellationToken);
        var videos = rows.OrderByDescending(v => v.PublishedAt).Select(v => v.ToPlaythrough()).ToList();
        return new PlaythroughBoard(videos, await sync.GetStatusAsync(db, cancellationToken));
    }

    public async Task<Playthrough> MoveAsync(string videoId, MoveRequest request, CancellationToken cancellationToken = default)
    {
        var video = await FindAsync(videoId, cancellationToken);
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        (video.State, video.Rating, video.WatchedOn) = PlaythroughRules.Move(video.State, video.Rating, video.WatchedOn, request, today);
        await db.SaveChangesAsync(cancellationToken);
        return video.ToPlaythrough();
    }

    public async Task<Playthrough> RateAsync(string videoId, decimal? rating, CancellationToken cancellationToken = default)
    {
        var video = await FindAsync(videoId, cancellationToken);
        video.Rating = PlaythroughRules.Rate(video.State, rating);
        await db.SaveChangesAsync(cancellationToken);
        return video.ToPlaythrough();
    }

    public Task<SyncStatus> SyncAsync(CancellationToken cancellationToken = default) => sync.SyncAsync(cancellationToken);

    private async Task<YouTubeVideoRow> FindAsync(string videoId, CancellationToken cancellationToken) =>
        await db.YouTubeVideos.FindAsync([videoId], cancellationToken)
        ?? throw new KeyNotFoundException($"No video {videoId} on the board.");
}
