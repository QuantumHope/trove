using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Trove.Web.Data;
using Trove.Web.YouTube;

namespace Trove.Tests;

/// <summary>
/// The real server on a throwaway SQLite file, with no YouTube key and no background sync, whatever the machine's
/// user secrets say.
/// </summary>
public sealed class TroveFactory : WebApplicationFactory<Program>
{
    private readonly string databaseFile = Path.Combine(Path.GetTempPath(), $"trove-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<TroveDbContext>>();
            services.RemoveAll<DbContextOptions<TroveDbContext>>();
            services.AddDbContext<TroveDbContext>(options => options.UseSqlite($"Data Source={databaseFile}"));

            var worker = services.Single(d => d.ImplementationType == typeof(YouTubeSyncWorker));
            services.Remove(worker);
            services.PostConfigure<YouTubeOptions>(options =>
            {
                options.ApiKey = null;
                options.Channels = ["@MKIceAndFire"];
            });
        });

    public async Task SeedAsync(params YouTubeVideoRow[] videos)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TroveDbContext>();
        db.YouTubeVideos.AddRange(videos);
        await db.SaveChangesAsync();
    }

    public async Task<YouTubeVideoRow?> FindAsync(string videoId)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<TroveDbContext>().YouTubeVideos.AsNoTracking()
            .SingleOrDefaultAsync(v => v.VideoId == videoId);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(databaseFile);
    }

    public static YouTubeVideoRow Video(string id, string title = "UNCHARTED 1 DRAKE'S FORTUNE Gameplay Walkthrough FULL GAME [4K 60FPS PS5] - No Commentary") => new()
    {
        VideoId = id,
        ChannelId = "UC1bwliGvJogr7cWK0nT2Eag",
        ChannelTitle = "MKIceAndFire",
        Title = title,
        PublishedAt = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero),
        DurationSeconds = 6 * 3600 + 12 * 60 + 5,
        ThumbnailUrl = "https://i.ytimg.com/vi/" + id + "/hqdefault.jpg",
        ViewCount = 1_234_567,
        State = Trove.Core.Playthroughs.WatchState.New,
        AddedAt = DateTimeOffset.UtcNow,
    };
}
