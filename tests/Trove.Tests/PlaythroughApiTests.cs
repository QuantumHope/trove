using System.Net;
using System.Net.Http.Json;
using Trove.Core.Playthroughs;

namespace Trove.Tests;

public sealed class PlaythroughApiTests : IAsyncLifetime
{
    private readonly TroveFactory factory = new();

    public async Task InitializeAsync()
    {
        factory.CreateClient().Dispose(); // start the server, which creates the database
        await factory.SeedAsync(TroveFactory.Video("vid-a"), TroveFactory.Video("vid-b"));
    }

    public Task DisposeAsync()
    {
        factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task The_board_lists_the_videos_and_says_no_key_is_set()
    {
        var board = await factory.CreateClient().GetFromJsonAsync<PlaythroughBoard>("/api/playthroughs");

        Assert.NotNull(board);
        Assert.Equal(["vid-a", "vid-b"], board.Videos.Select(v => v.VideoId).Order());
        Assert.False(board.Sync.HasApiKey);
        Assert.Equal(["@MKIceAndFire"], board.Sync.Channels);
        Assert.Equal(TimeSpan.FromSeconds(22325), board.Videos[0].Duration);
    }

    [Fact]
    public async Task Moving_to_done_saves_the_rating_and_the_day_watched()
    {
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/playthroughs/vid-a/move", new MoveRequest(WatchState.InProgress, null));

        var response = await client.PostAsJsonAsync("/api/playthroughs/vid-a/move", new MoveRequest(WatchState.Done, 4.75m));

        response.EnsureSuccessStatusCode();
        var saved = await factory.FindAsync("vid-a");
        Assert.Equal(WatchState.Done, saved!.State);
        Assert.Equal(4.75m, saved.Rating);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), saved.WatchedOn);
    }

    [Fact]
    public async Task Ratings_can_change_while_in_progress_but_not_before()
    {
        var client = factory.CreateClient();

        var tooSoon = await client.PutAsJsonAsync("/api/playthroughs/vid-b/rating", new RateRequest(3m));
        await client.PostAsJsonAsync("/api/playthroughs/vid-b/move", new MoveRequest(WatchState.InProgress, null));
        var rated = await client.PutAsJsonAsync("/api/playthroughs/vid-b/rating", new RateRequest(2.25m));
        var offStep = await client.PutAsJsonAsync("/api/playthroughs/vid-b/rating", new RateRequest(2.3m));

        Assert.Equal(HttpStatusCode.BadRequest, tooSoon.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rated.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, offStep.StatusCode);
        Assert.Equal(2.25m, (await factory.FindAsync("vid-b"))!.Rating);
    }

    [Fact]
    public async Task Unknown_videos_are_not_found()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/playthroughs/nope/move", new MoveRequest(WatchState.Ignored, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_page_prerenders_cards_that_open_youtube()
    {
        var html = await factory.CreateClient().GetStringAsync("/library/youtube");

        Assert.Contains("href=\"https://www.youtube.com/watch?v=vid-a\"", html, StringComparison.Ordinal);
        Assert.Contains("6:12:05", html, StringComparison.Ordinal);
        Assert.Contains("Add a YouTube API key", html, StringComparison.Ordinal);
    }
}
