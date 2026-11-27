using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Playthroughs;
using Trove.UI.Pages;

namespace Trove.Tests;

public sealed class PlaythroughBoardTests : BunitContext
{
    private readonly FakeLibrary library = new();

    public PlaythroughBoardTests()
    {
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IPlaythroughLibrary>(library);
    }

    [Fact]
    public void Moving_to_done_asks_for_a_rating_first()
    {
        var page = Render<Playthroughs>();

        page.Find("[data-video=a] select").Change(nameof(WatchState.Done));

        Assert.Empty(library.Moves);
        Assert.Contains("Rate it", page.Find("[role=dialog]").TextContent, StringComparison.Ordinal);

        page.Find("[role=dialog] .trove-stars").Click(new Microsoft.AspNetCore.Components.Web.MouseEventArgs { OffsetX = 90 });
        page.FindAll("[role=dialog] button").Single(b => b.TextContent == "Save").Click();

        Assert.Equal(("a", new MoveRequest(WatchState.Done, 3.75m)), library.Moves.Single());
        Assert.Empty(page.FindAll("[role=dialog]"));
        Assert.Contains("Watched", page.Find("[data-video=a]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Skipping_marks_it_done_unrated_and_cancel_leaves_it_alone()
    {
        var page = Render<Playthroughs>();

        page.Find("[data-video=a] select").Change(nameof(WatchState.Done));
        page.FindAll("[role=dialog] button").Single(b => b.TextContent == "Cancel").Click();
        Assert.Empty(library.Moves);

        page.Find("[data-video=a] select").Change(nameof(WatchState.Done));
        page.FindAll("[role=dialog] button").Single(b => b.TextContent == "Skip").Click();
        Assert.Equal(("a", new MoveRequest(WatchState.Done, null)), library.Moves.Single());
    }

    [Fact]
    public void Other_moves_happen_straight_away_and_stars_show_in_progress()
    {
        var page = Render<Playthroughs>();

        page.Find("[data-video=a] select").Change(nameof(WatchState.InProgress));

        Assert.Equal(("a", new MoveRequest(WatchState.InProgress, null)), library.Moves.Single());
        Assert.NotNull(page.Find("[data-video=a] [role=slider]"));
        Assert.Equal("https://www.youtube.com/watch?v=a", page.Find("[data-video=a] .trove-video-title").GetAttribute("href"));
    }

    [Fact]
    public void A_board_that_fails_to_load_says_why_and_tries_again()
    {
        library.LoadFailure = "The Trove module is turned off in CORA (Admin → Modules).";
        var page = Render<Playthroughs>();

        Assert.Contains("turned off in CORA", page.Find("[role=alert]").TextContent, StringComparison.Ordinal);

        library.LoadFailure = null;
        page.FindAll("button").Single(b => b.TextContent == "Try again").Click();

        Assert.NotNull(page.Find("[data-video=a]"));
    }

    [Fact]
    public void Without_a_key_the_board_points_to_cora()
    {
        library.HasApiKey = false;
        var page = Render<Playthroughs>();

        Assert.Equal("/admin/modules", page.FindAll("a").Single(a => a.TextContent == "Open CORA").GetAttribute("href"));
    }

    private sealed class FakeLibrary : IPlaythroughLibrary
    {
        public string? LoadFailure { get; set; }

        public bool HasApiKey { get; set; } = true;

        private readonly Dictionary<string, Playthrough> videos = new()
        {
            ["a"] = new("a", "UNCHARTED 1 DRAKE'S FORTUNE Gameplay Walkthrough FULL GAME", "MKIceAndFire", DateTimeOffset.UtcNow.AddDays(-400),
                TimeSpan.FromHours(6), "", 1_000_000, WatchState.New, null, null),
        };

        public List<(string, MoveRequest)> Moves { get; } = [];

        public Task<PlaythroughBoard> GetBoardAsync(CancellationToken cancellationToken = default) =>
            LoadFailure is { } failure
                ? Task.FromException<PlaythroughBoard>(new HttpRequestException(failure))
                : Task.FromResult(new PlaythroughBoard([.. videos.Values], new SyncStatus(HasApiKey, ["@MKIceAndFire"], DateTimeOffset.UtcNow, null)));

        public Task<Playthrough> MoveAsync(string videoId, MoveRequest request, CancellationToken cancellationToken = default)
        {
            Moves.Add((videoId, request with { Today = null }));
            Assert.NotNull(request.Today);
            var v = videos[videoId];
            var (state, rating, watched) = PlaythroughRules.Move(v.State, v.Rating, v.WatchedOn, request, DateOnly.FromDateTime(DateTime.Today));
            return Task.FromResult(videos[videoId] = v with { State = state, Rating = rating, WatchedOn = watched });
        }

        public Task<Playthrough> RateAsync(string videoId, decimal? rating, CancellationToken cancellationToken = default) =>
            Task.FromResult(videos[videoId] = videos[videoId] with { Rating = rating });

        public Task<SyncStatus> SyncAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SyncStatus(true, ["@MKIceAndFire"], DateTimeOffset.UtcNow, null));
    }
}
