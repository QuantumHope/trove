using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Playthroughs;
using Trove.UI.Pages;

namespace Trove.Tests;

public sealed class WatchTests : BunitContext
{
    private const string Player = "./_content/Trove.UI/player.js";

    private readonly PlaythroughBoardTests.FakeLibrary library = new();
    private readonly BunitJSModuleInterop module;

    public WatchTests()
    {
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IPlaythroughLibrary>(library);
        module = JSInterop.SetupModule(Player);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<int>("resumeAt", _ => true).SetResult(0);
    }

    private IRenderedComponent<Watch> Open(string id = "a") => Render<Watch>(p => p.Add(w => w.VideoId, id));

    [Fact]
    public void The_player_waits_for_play_and_youtube_is_always_a_tap_away()
    {
        var page = Open();

        Assert.Contains("UNCHARTED 1", page.Find("h1").TextContent, StringComparison.Ordinal);
        Assert.NotNull(page.Find(".trove-player-poster"));
        Assert.DoesNotContain(module.Invocations, i => i.Identifier == "mount");
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == "https://www.youtube.com/watch?v=a" && a.GetAttribute("target") == "_blank");
    }

    [Fact]
    public void A_saved_spot_shows_on_the_play_button()
    {
        module.Setup<int>("resumeAt", _ => true).SetResult(3_725);
        var page = Open();

        page.WaitForAssertion(() => Assert.Contains("Pick up at 1:02:05", page.Find(".trove-player-hint").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void Pressing_play_loads_the_player_and_starting_moves_it_to_in_progress_with_undo()
    {
        var page = Open();

        page.Find(".trove-player-poster").Click();
        Assert.Single(module.Invocations, i => i.Identifier == "mount");
        Assert.Empty(page.FindAll(".trove-player-poster"));

        page.InvokeAsync(() => page.Instance.OnPlaying());
        Assert.Equal(("a", new MoveRequest(WatchState.InProgress, null)), library.Moves.Single());
        Assert.Contains("Moved to In progress", page.Find(".trove-viewer-note").TextContent, StringComparison.Ordinal);

        page.Find(".trove-viewer-note button").Click();
        Assert.Equal(("a", new MoveRequest(WatchState.New, null)), library.Moves[1]);

        // Pausing and playing again doesn't move it a second time.
        page.InvokeAsync(() => page.Instance.OnPlaying());
        Assert.Equal(3, library.Moves.Count);
    }

    [Theory]
    [InlineData(150, "only plays on YouTube")]
    [InlineData(101, "only plays on YouTube")]
    [InlineData(100, "can't find this video")]
    [InlineData(5, "couldn't play this here")]
    public void A_video_the_player_cant_show_says_why_and_offers_youtube(int code, string says)
    {
        var page = Open();
        page.Find(".trove-player-poster").Click();

        page.InvokeAsync(() => page.Instance.OnPlayerError(code));

        var panel = page.Find(".trove-player-blocked");
        Assert.Contains(says, panel.TextContent, StringComparison.Ordinal);
        Assert.Equal("https://www.youtube.com/watch?v=a", panel.QuerySelector("a")!.GetAttribute("href"));
    }

    [Fact]
    public void Mark_done_asks_for_a_rating_and_forgets_the_saved_spot()
    {
        var page = Open();

        page.Find(".trove-viewer-actions .is-primary").Click();
        page.Find("[role=dialog] .is-quiet + button").Click();

        Assert.Equal(("a", new MoveRequest(WatchState.Done, null)), library.Moves.Single());
        Assert.Contains("Watched", page.Find(".trove-viewer-state").TextContent, StringComparison.Ordinal);
        Assert.Single(module.Invocations, i => i.Identifier == "forget");
    }

    [Fact]
    public void The_end_of_the_video_offers_mark_done()
    {
        var page = Open();
        page.Find(".trove-player-poster").Click();

        page.InvokeAsync(() => page.Instance.OnEnded());

        Assert.Contains("end of the video", page.Find(".trove-viewer-note").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_video_says_so()
    {
        var page = Open("zzz");

        Assert.Contains("isn't on your board", page.Find("h1").TextContent, StringComparison.Ordinal);
    }
}
