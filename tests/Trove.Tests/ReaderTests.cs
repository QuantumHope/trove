using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Articles;
using Trove.Core.Viewing;
using Trove.UI.Pages;
using Trove.UI.Viewing;

namespace Trove.Tests;

public sealed class ReaderTests : BunitContext
{
    private readonly ArticleBoardTests.FakeLibrary library = new();
    private readonly FakeFrames frames = new();

    public ReaderTests()
    {
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IArticleLibrary>(library);
        Services.AddSingleton<IFrameChecker>(frames);
    }

    private IRenderedComponent<Read> Open(string id) => Render<Read>(p => p.Add(r => r.Id, id));

    [Fact]
    public void A_page_that_allows_frames_shows_sandboxed_with_no_referrer_and_a_new_tab_link()
    {
        var page = Open("saved");

        var frame = page.Find(".trove-reader-frame iframe");
        Assert.Equal("https://example.com/saved", frame.GetAttribute("src"));
        Assert.Equal("no-referrer", frame.GetAttribute("referrerpolicy"));
        var sandbox = frame.GetAttribute("sandbox")!;
        Assert.Contains("allow-scripts", sandbox, StringComparison.Ordinal);
        Assert.DoesNotContain("allow-top-navigation", sandbox, StringComparison.Ordinal);
        Assert.Equal("https://example.com/saved", page.Find(".trove-reader-newtab").GetAttribute("href"));
        Assert.Equal("_blank", page.Find(".trove-reader-newtab").GetAttribute("target"));
        Assert.Equal(["https://example.com/saved"], frames.Checked);
    }

    [Fact]
    public void Opening_moves_it_to_reading_and_undo_puts_it_back()
    {
        var page = Open("saved");

        Assert.Equal(("saved", ReadState.Reading), (library.Moves[0].Id, library.Moves[0].Request.State));
        Assert.Contains("Moved to Reading", page.Find(".trove-viewer-moved").TextContent, StringComparison.Ordinal);

        page.Find(".trove-viewer-moved button").Click();

        Assert.Equal(ReadState.ReadNext, library.Moves[1].Request.State);
        Assert.Empty(page.FindAll(".trove-viewer-moved"));
    }

    [Fact]
    public void Try_again_after_a_failed_load_opens_the_article_fully()
    {
        library.LoadFailure = "CORA is restarting.";
        var page = Open("saved");
        Assert.Contains("didn't load", page.Find("[role=alert]").TextContent, StringComparison.Ordinal);

        library.LoadFailure = null;
        page.Find("[role=alert] button").Click();

        Assert.Equal("https://example.com/saved", page.Find(".trove-reader-frame iframe").GetAttribute("src"));
        Assert.Equal(ReadState.Reading, library.Moves.Single().Request.State);
    }

    [Fact]
    public void An_article_already_being_read_isnt_moved_and_can_be_marked_read()
    {
        library.Extra = ArticleBoardTests.Saved("reading", "https://example.com/r", "Halfway", ReadState.Reading);
        var page = Open("reading");

        Assert.Empty(library.Moves);
        var forward = page.Find(".trove-reader-forward");
        forward.Click();
        Assert.Equal(ReadState.Done, library.Moves.Single().Request.State);
    }

    [Fact]
    public void A_site_that_refuses_frames_says_why_and_offers_a_new_tab_and_coras_copy()
    {
        frames.Answer = Framing.Blocked;
        library.Knowledge["saved"] = new CoraKnowledge(KnowledgeState.InCora, "doc1");
        var page = Open("saved");

        Assert.Empty(page.FindAll("iframe"));
        var panel = page.Find(".trove-reader-blocked");
        Assert.Contains("doesn't let other apps", panel.TextContent, StringComparison.Ordinal);
        Assert.Contains(panel.QuerySelectorAll("a"), a => a.GetAttribute("href") == "https://example.com/saved" && a.GetAttribute("target") == "_blank");
        Assert.Contains(panel.QuerySelectorAll("a"), a => a.GetAttribute("href") == "/knowledge/library/doc1" && a.TextContent.Contains("CORA's copy", StringComparison.Ordinal));
    }

    [Fact]
    public void When_cora_cant_tell_the_frame_is_tried_with_a_warning()
    {
        frames.Answer = Framing.Unknown;
        var page = Open("saved");

        Assert.NotNull(page.Find("iframe"));
        Assert.Contains("may stay blank", page.Find(".trove-reader-help").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_page_on_trove_s_own_address_is_never_framed_or_checked()
    {
        library.Extra = ArticleBoardTests.Saved("own", "http://localhost/admin/feeds", "CORA's feeds", ReadState.ReadNext);
        var page = Open("own");

        Assert.Empty(page.FindAll("iframe"));
        Assert.Empty(frames.Checked);
        Assert.Contains("part of CORA", page.Find(".trove-reader-blocked").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_article_says_so()
    {
        var page = Open("nope");

        Assert.Contains("isn't on your list", page.Find("h1").TextContent, StringComparison.Ordinal);
        Assert.Empty(library.Moves);
    }

    private sealed class FakeFrames : IFrameChecker
    {
        public Framing Answer { get; set; } = Framing.Allowed;

        public List<string> Checked { get; } = [];

        public Task<FrameCheck> CheckAsync(string url, CancellationToken cancellationToken = default)
        {
            Checked.Add(url);
            return Task.FromResult(new FrameCheck(url, Answer, Answer == Framing.Blocked ? "example.com doesn't let other apps show its pages in a frame." : null));
        }
    }
}

public sealed class ReaderRulesTests
{
    private static readonly Uri App = new("https://cora.tail.ts.net/trove/");

    [Theory]
    [InlineData("https://example.com/a", null)]
    [InlineData("http://example.com/a", "isn't served over https")]
    [InlineData("https://cora.tail.ts.net/knowledge/library/1", "part of CORA")]
    [InlineData("https://CORA.tail.ts.net:443/knowledge/library/1", "part of CORA")]
    [InlineData("https://someone@cora.tail.ts.net/admin", "carries a sign-in")]
    [InlineData("https://cora.tail.ts.net:8443/other-app", null)]
    [InlineData("ftp://example.com/a", "isn't a web page")]
    [InlineData("not a link", "isn't a web page")]
    public void Trove_frames_only_https_pages_on_other_sites(string url, string? reason)
    {
        var said = ReaderRules.WontFrame(url, App);
        if (reason is null)
        {
            Assert.Null(said);
        }
        else
        {
            Assert.Contains(reason, said, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(ReadState.New, ReadState.Reading)]
    [InlineData(ReadState.ReadNext, ReadState.Reading)]
    [InlineData(ReadState.Reading, null)]
    [InlineData(ReadState.Done, null)]
    [InlineData(ReadState.Ignored, null)]
    public void Opening_an_article_moves_it_only_from_the_queue(ReadState from, ReadState? to) =>
        Assert.Equal(to, ReaderRules.OnOpen(from));

    [Theory]
    [InlineData(Core.Playthroughs.WatchState.New, Core.Playthroughs.WatchState.InProgress)]
    [InlineData(Core.Playthroughs.WatchState.WatchNext, Core.Playthroughs.WatchState.InProgress)]
    [InlineData(Core.Playthroughs.WatchState.InProgress, null)]
    [InlineData(Core.Playthroughs.WatchState.Done, null)]
    public void Playing_a_video_moves_it_only_from_the_queue(Core.Playthroughs.WatchState from, Core.Playthroughs.WatchState? to) =>
        Assert.Equal(to, ReaderRules.OnPlay(from));
}

public sealed class HttpFrameCheckerTests
{
    [Fact]
    public async Task Coras_answer_is_read_from_its_own_root_route()
    {
        var handler = new Handler(HttpStatusCode.OK, """{"url":"https://example.com/a","framing":"blocked","reason":"No frames."}""");
        var checker = new HttpFrameChecker(new HttpClient(handler) { BaseAddress = new Uri("https://cora.example/trove/") });

        var check = await checker.CheckAsync("https://example.com/a?b=1&c=2");

        Assert.Equal(new FrameCheck("https://example.com/a", Framing.Blocked, "No frames."), check);
        Assert.Equal("https://cora.example/web/frame-check?url=https%3A%2F%2Fexample.com%2Fa%3Fb%3D1%26c%3D2", handler.Asked!.ToString());
    }

    [Fact]
    public async Task A_refusal_or_an_outage_is_unknown_not_an_error()
    {
        var refused = new HttpFrameChecker(new HttpClient(new Handler(HttpStatusCode.BadRequest, """{"detail":"That address is on a private network."}""")) { BaseAddress = new Uri("https://cora.example/trove/") });
        var down = new HttpFrameChecker(new HttpClient(new Handler(null, "")) { BaseAddress = new Uri("https://cora.example/trove/") });

        var a = await refused.CheckAsync("https://10.0.0.1/");
        var b = await down.CheckAsync("https://example.com/");

        Assert.Equal(Framing.Unknown, a.Framing);
        Assert.Contains("private network", a.Reason, StringComparison.Ordinal);
        Assert.Equal(Framing.Unknown, b.Framing);
    }

    [Fact]
    public async Task An_answer_without_a_verdict_is_unknown()
    {
        var checker = new HttpFrameChecker(new HttpClient(new Handler(HttpStatusCode.OK, """{"url":"https://example.com/a"}""")) { BaseAddress = new Uri("https://cora.example/trove/") });

        Assert.Equal(Framing.Unknown, (await checker.CheckAsync("https://example.com/a")).Framing);
    }

    [Fact]
    public async Task A_check_that_takes_too_long_is_unknown()
    {
        var checker = new HttpFrameChecker(new HttpClient(new Hanging()) { BaseAddress = new Uri("https://cora.example/trove/"), Timeout = TimeSpan.FromMilliseconds(50) });

        var check = await checker.CheckAsync("https://example.com/a");

        Assert.Equal(Framing.Unknown, check.Framing);
    }

    private sealed class Hanging : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("unreachable");
        }
    }

    private sealed class Handler(HttpStatusCode? status, string body) : HttpMessageHandler
    {
        public Uri? Asked { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Asked = request.RequestUri;
            return status is { } code
                ? Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") })
                : throw new HttpRequestException("CORA is down.");
        }
    }
}
