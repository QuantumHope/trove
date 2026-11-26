using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Trove.Core.Playthroughs;
using Trove.Core.Settings;
using Trove.Web.Data;
using Trove.Web.YouTube;

namespace Trove.Tests;

public sealed class YouTubeSyncTests : IDisposable
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"trove-sync-{Guid.NewGuid():N}.db");
    private readonly FakeYouTube youtube = new();
    private readonly ServiceProvider services;

    public YouTubeSyncTests()
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddDbContext<TroveDbContext>(o => o.UseSqlite($"Data Source={file}"));
        collection.Configure<YouTubeOptions>(o =>
        {
            o.ApiKey = "test-key";
            o.Channels = ["@MKIceAndFire"];
        });
        collection.AddSingleton(TimeProvider.System);
        collection.AddDataProtection().UseEphemeralDataProtectionProvider();
        collection.AddSingleton<YouTubeKeyStore>();
        collection.AddScoped<YouTubeSettingsService>();
        collection.AddHttpClient<YouTubeDataApi>().ConfigurePrimaryHttpMessageHandler(() => youtube);
        collection.AddSingleton<YouTubeSync>();
        services = collection.BuildServiceProvider();
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TroveDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task First_sync_reads_every_page_and_keeps_only_full_games()
    {
        var status = await services.GetRequiredService<YouTubeSync>().SyncAsync(CancellationToken.None);

        Assert.Null(status.LastError);
        Assert.NotNull(status.LastSyncedAt);
        var videos = await Videos();
        Assert.Equal(["full-new", "full-old"], videos.Select(v => v.VideoId).Order());
        var full = videos.Single(v => v.VideoId == "full-old");
        Assert.Equal(10 * 3600 + 4 * 60 + 31, full.DurationSeconds);
        Assert.Equal(2_500_000, full.ViewCount);
        Assert.Equal("https://i.ytimg.com/vi/full-old/hqdefault.jpg", full.ThumbnailUrl);
        Assert.Equal(WatchState.New, full.State);
        Assert.All(youtube.Requests, r => Assert.Equal("test-key", r.Headers.GetValues("X-Goog-Api-Key").Single()));
        Assert.All(youtube.Requests, r => Assert.DoesNotContain("key=", r.RequestUri!.Query, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Later_syncs_stop_at_uploads_already_seen_and_keep_your_states()
    {
        var sync = services.GetRequiredService<YouTubeSync>();
        await sync.SyncAsync(CancellationToken.None);
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TroveDbContext>();
            (await db.YouTubeVideos.FindAsync("full-old"))!.State = WatchState.Done;
            await db.SaveChangesAsync();
        }

        youtube.Requests.Clear();
        youtube.NewUpload = true;
        await sync.SyncAsync(CancellationToken.None);

        Assert.DoesNotContain(youtube.Requests, r => r.RequestUri!.Query.Contains("pageToken", StringComparison.Ordinal));
        var videos = await Videos();
        Assert.Contains(videos, v => v.VideoId == "full-newest");
        Assert.Equal(WatchState.Done, videos.Single(v => v.VideoId == "full-old").State);
    }

    [Fact]
    public async Task A_refused_key_is_reported_in_plain_words()
    {
        youtube.Refuse = true;

        var status = await services.GetRequiredService<YouTubeSync>().SyncAsync(CancellationToken.None);

        Assert.Contains("refused the key", status.LastError, StringComparison.Ordinal);
        Assert.Empty(await Videos());
    }

    [Theory]
    [InlineData("PT10H4M31S", 36271)]
    [InlineData("PT45M", 2700)]
    [InlineData("P1DT2H", 93600)]
    [InlineData("", 0)]
    [InlineData("nonsense", 0)]
    public void Durations_parse_from_iso_8601(string duration, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), YouTubeDataApi.ParseDuration(duration));

    public void Dispose()
    {
        services.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(file);
    }

    [Fact]
    public async Task A_key_saved_in_settings_is_tested_encrypted_and_used_instead_of_configuration()
    {
        using var scope = services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<YouTubeSettingsService>();
        Assert.Equal(YouTubeKeySource.Configuration, (await settings.GetAsync()).Source);

        var result = await settings.SaveKeyAsync("  saved-key-WXYZ ");

        Assert.True(result.Saved);
        Assert.Equal(YouTubeKeySource.Saved, result.Settings.Source);
        Assert.Equal("WXYZ", result.Settings.KeyEnding);
        Assert.Equal("saved-key-WXYZ", youtube.Requests[0].Headers.GetValues("X-Goog-Api-Key").Single());
        var stored = await scope.ServiceProvider.GetRequiredService<TroveDbContext>().AppSettings.AsNoTracking().SingleAsync();
        Assert.DoesNotContain("saved-key", stored.Value, StringComparison.Ordinal);
        Assert.Equal(("saved-key-WXYZ", YouTubeKeySource.Saved), await services.GetRequiredService<YouTubeKeyStore>().GetAsync());

        var removed = await settings.RemoveKeyAsync();
        Assert.Equal(YouTubeKeySource.Configuration, removed.Source);
        Assert.Equal("-key", removed.KeyEnding);
    }

    [Fact]
    public async Task A_key_youtube_refuses_is_not_saved()
    {
        youtube.Refuse = true;
        using var scope = services.CreateScope();

        var result = await scope.ServiceProvider.GetRequiredService<YouTubeSettingsService>().SaveKeyAsync("bad-key");

        Assert.False(result.Saved);
        Assert.Contains("refused the key", result.Error, StringComparison.Ordinal);
        Assert.Equal(YouTubeKeySource.Configuration, result.Settings.Source);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<TroveDbContext>().AppSettings);
    }

    private async Task<List<YouTubeVideoRow>> Videos()
    {
        using var scope = services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<TroveDbContext>().YouTubeVideos.AsNoTracking().ToListAsync();
    }

    /// <summary>Canned YouTube Data API answers: two pages of uploads, newest first.</summary>
    private sealed class FakeYouTube : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public bool NewUpload { get; set; }

        public bool Refuse { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (Refuse)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
            }

            var path = request.RequestUri!.AbsolutePath;
            var query = request.RequestUri.Query;
            var json = path.EndsWith("/channels", StringComparison.Ordinal)
                ? """{"items":[{"id":"UC1bwliGvJogr7cWK0nT2Eag","snippet":{"title":"MKIceAndFire","channelTitle":"","publishedAt":"2012-01-01T00:00:00Z","thumbnails":{}},"contentDetails":{"relatedPlaylists":{"uploads":"UU1bwliGvJogr7cWK0nT2Eag"}}}]}"""
                : path.EndsWith("/playlistItems", StringComparison.Ordinal)
                    ? query.Contains("pageToken=p2", StringComparison.Ordinal) ? PageTwo : PageOne()
                    : Details(query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }

        private string PageOne() =>
            "{\"nextPageToken\":\"p2\",\"items\":[" +
            (NewUpload ? Item("full-newest", "NEW GAME Gameplay Walkthrough FULL GAME [4K] - No Commentary", "2026-11-25T00:00:00Z") + "," : "") +
            Item("full-new", "UNCHARTED 1 DRAKE'S FORTUNE Gameplay Walkthrough FULL GAME [4K 60FPS PS5] - No Commentary", "2026-10-01T00:00:00Z") + "," +
            Item("part-1", "STARFIELD Gameplay Walkthrough Part 1 FULL GAME [4K 60FPS] - No Commentary", "2026-09-01T00:00:00Z") + "]}";

        private static readonly string PageTwo =
            "{\"items\":[" +
            Item("full-old", "THE LAST OF US PART I Gameplay Walkthrough FULL GAME", "2025-01-01T00:00:00Z") + "," +
            Item("trailer", "SOME GAME Trailer", "2024-12-01T00:00:00Z") + "]}";

        private static string Item(string id, string title, string published) =>
            $$$"""{"snippet":{"title":"{{{title}}}","channelTitle":"MKIceAndFire","publishedAt":"{{{published}}}","thumbnails":{}},"contentDetails":{"videoId":"{{{id}}}","videoPublishedAt":"{{{published}}}"}}""";

        private static string Details(string query)
        {
            var ids = Uri.UnescapeDataString(query.Split("id=")[1]).Split(',');
            var items = ids.Select(id =>
            {
                var title = id == "full-old" ? "THE LAST OF US Gameplay Walkthrough FULL GAME" : "X Walkthrough FULL GAME";
                return $$$$"""{"id":"{{{{id}}}}","snippet":{"title":"{{{{title}}}}","channelTitle":"MKIceAndFire","publishedAt":"2025-01-01T00:00:00Z","liveBroadcastContent":"none","thumbnails":{"medium":{"url":"https://i.ytimg.com/vi/{{{{id}}}}/mqdefault.jpg"},"high":{"url":"https://i.ytimg.com/vi/{{{{id}}}}/hqdefault.jpg"}}},"contentDetails":{"duration":"PT10H4M31S"},"statistics":{"viewCount":"2500000"}}""";
            });
            return "{\"items\":[" + string.Join(',', items) + "]}";
        }
    }
}
