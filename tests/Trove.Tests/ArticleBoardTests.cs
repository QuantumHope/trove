using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Articles;
using Trove.UI.Pages;

namespace Trove.Tests;

public sealed class ArticleBoardTests : BunitContext
{
    private static readonly DateTimeOffset Added = DateTimeOffset.Parse("2026-11-20T12:00:00Z");

    private readonly FakeLibrary library = new();

    public ArticleBoardTests()
    {
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IArticleLibrary>(library);
    }

    [Fact]
    public void A_pasted_link_is_saved_and_shows_in_read_next()
    {
        var page = Render<Articles>();

        page.Find("#article-url").Input("https://example.com/new");
        page.Find("#article-title").Input("  A new one  ");
        page.Find("form").Submit();

        Assert.Equal(new AddArticleRequest("https://example.com/new", "A new one", AddToCora: false), library.Adds.Single());
        Assert.Contains("Added to Read next", page.Find("[role=status]").TextContent, StringComparison.Ordinal);
        Assert.Contains("A new one", page.Find("[aria-label='Read next']").TextContent, StringComparison.Ordinal);
        Assert.Equal("", page.Find("#article-url").GetAttribute("value"));
    }

    [Fact]
    public void A_link_already_on_the_list_says_so_and_is_not_shown_twice()
    {
        var page = Render<Articles>();

        page.Find("#article-url").Input("https://example.com/saved");
        page.Find("form").Submit();

        Assert.Contains("already on your list", page.Find("[role=status]").TextContent, StringComparison.Ordinal);
        Assert.Single(page.FindAll("[data-article=saved]"));
    }

    [Fact]
    public void A_refused_link_shows_the_modules_reason()
    {
        library.AddFailure = "That isn't a web address. Links start with https:// or http://.";
        var page = Render<Articles>();

        page.Find("#article-url").Input("not a link");
        page.Find("form").Submit();

        Assert.Contains("Links start with https://", page.Find("[role=alert]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Moving_to_done_sends_the_persons_date_and_shows_when_it_was_read()
    {
        var page = Render<Articles>();

        page.Find("[data-article=saved] select").Change(nameof(ReadState.Done));

        var (id, request) = library.Moves.Single();
        Assert.Equal("saved", id);
        Assert.Equal(ReadState.Done, request.State);
        Assert.NotNull(request.Today);
        Assert.Contains("Read ", page.Find("[aria-label=Done] [data-article=saved]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Only_saved_links_offer_remove()
    {
        var page = Render<Articles>();

        Assert.NotEmpty(page.FindAll("[data-article=saved] .trove-article-remove"));
        Assert.Empty(page.FindAll("[data-article=feed] .trove-article-remove"));

        page.Find("[data-article=saved] .trove-article-remove").Click();

        Assert.Equal("saved", library.Removes.Single());
        Assert.Empty(page.FindAll("[data-article=saved]"));
    }

    [Fact]
    public void A_feed_article_names_its_feed_and_opens_in_a_new_tab()
    {
        var page = Render<Articles>();

        var card = page.Find("[data-article=feed]");
        Assert.Contains("Example feed", card.TextContent, StringComparison.Ordinal);
        var link = page.Find("[data-article=feed] .trove-article-title");
        Assert.Equal("https://feed.example.com/post", link.GetAttribute("href"));
        Assert.Equal("_blank", link.GetAttribute("target"));
        Assert.Contains("noopener", link.GetAttribute("rel"), StringComparison.Ordinal);
    }

    [Fact]
    public void A_list_that_fails_to_load_says_why_and_tries_again()
    {
        library.LoadFailure = "The Trove module is not in use.";
        var page = Render<Articles>();

        Assert.Contains("not in use", page.Find("[role=alert]").TextContent, StringComparison.Ordinal);

        library.LoadFailure = null;
        page.FindAll("button").Single(b => b.TextContent == "Try again").Click();

        Assert.NotNull(page.Find("[data-article=saved]"));
    }

    [Fact]
    public void Feed_posts_that_did_not_come_through_say_why_and_where_feeds_are_kept()
    {
        library.Feeds = new FeedSyncStatus(Connected: true, LastSyncedAt: Added, Error: "CORA couldn't be reached for new feed posts.");
        var page = Render<Articles>();

        var banner = page.Find("[role=alert]");
        Assert.Contains("couldn't be reached", banner.TextContent, StringComparison.Ordinal);
        Assert.Equal("/admin/feeds", banner.QuerySelector("a")!.GetAttribute("href"));
        Assert.NotNull(page.Find("[data-article=saved]"));
    }

    [Fact]
    public void A_healthy_feed_copy_shows_no_banner_and_links_to_feeds_in_cora()
    {
        library.Feeds = new FeedSyncStatus(Connected: true, LastSyncedAt: Added, Error: null);
        var page = Render<Articles>();

        Assert.Empty(page.FindAll("[role=alert]"));
        Assert.Contains(page.FindAll("a"), a => a.GetAttribute("href") == "/admin/feeds" && a.TextContent.Contains("Feeds in CORA", StringComparison.Ordinal));
    }

    [Fact]
    public void A_feed_named_for_its_author_names_them_once()
    {
        library.Extra = Saved("blog", "https://jvns.ca/blog/dns", "DNS", ReadState.New, ArticleSource.Feed, "Julia Evans") with { Author = "Julia Evans" };
        var page = Render<Articles>();

        var meta = page.Find("[data-article=blog]").TextContent;
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(meta, "Julia Evans"));
    }

    [Fact]
    public void Each_card_says_whether_CORA_has_read_it()
    {
        library.Knowledge["saved"] = new CoraKnowledge(KnowledgeState.InCora, "c1", "applied");
        library.Knowledge["feed"] = new CoraKnowledge(KnowledgeState.Queued, "c2", "open");
        var page = Render<Articles>();

        var saved = page.Find("[data-article=saved] .trove-article-cora");
        Assert.Contains("In CORA", saved.TextContent, StringComparison.Ordinal);
        var view = saved.QuerySelector("a")!;
        Assert.Equal("View in CORA", view.TextContent.Trim());
        Assert.Equal("/knowledge/library/c1", view.GetAttribute("href"));
        Assert.Null(saved.QuerySelector("button"));

        var feed = page.Find("[data-article=feed] .trove-article-cora");
        Assert.Contains("Ready to review", feed.TextContent, StringComparison.Ordinal);
        var review = feed.QuerySelector("a")!;
        Assert.Equal("Review in CORA", review.TextContent.Trim());
        Assert.Equal("/admin/changesets/c2", review.GetAttribute("href"));
    }

    [Fact]
    public void An_article_CORA_hasnt_read_can_be_added_from_its_card()
    {
        library.Knowledge["saved"] = new CoraKnowledge(KnowledgeState.NotInCora);
        var page = Render<Articles>();

        page.Find("[data-article=saved] .trove-cora-add").Click();

        Assert.Equal(["saved"], library.CoraAdds);
        var cora = page.Find("[data-article=saved] .trove-article-cora");
        Assert.Contains("Needs your check", cora.TextContent, StringComparison.Ordinal);
        Assert.Equal("/admin/changesets/c9/document", cora.QuerySelector("a")!.GetAttribute("href"));
        Assert.Null(cora.QuerySelector("button"));
        var status = page.Find("[role=status]");
        Assert.Contains("Check its text there, then press Analyze", status.TextContent, StringComparison.Ordinal);
        Assert.Equal("/admin/changesets/c9/document", status.QuerySelector("a")!.GetAttribute("href"));
    }

    [Fact]
    public void A_page_CORA_is_still_reading_or_bringing_in_offers_only_what_fits()
    {
        library.Knowledge["saved"] = new CoraKnowledge(KnowledgeState.Queued, "c4", "drafting");
        library.Knowledge["feed"] = new CoraKnowledge(KnowledgeState.Queued);
        var page = Render<Articles>();

        var saved = page.Find("[data-article=saved] .trove-article-cora");
        Assert.Contains("CORA is reading", saved.TextContent, StringComparison.Ordinal);
        Assert.Equal("/admin/changesets/c4", saved.QuerySelector("a")!.GetAttribute("href"));

        var feed = page.Find("[data-article=feed] .trove-article-cora");
        Assert.Contains("On its way to CORA", feed.TextContent, StringComparison.Ordinal);
        Assert.Null(feed.QuerySelector("a"));
        Assert.Null(feed.QuerySelector("button"));
    }

    [Fact]
    public void Each_card_offers_the_next_step_as_one_button()
    {
        var page = Render<Articles>();

        page.Find("[data-article=saved] .trove-article-forward").Click();
        Assert.Equal(("saved", ReadState.Reading), (library.Moves.Last().Id, library.Moves.Last().Request.State));

        page.Find("[data-article=feed] .trove-article-forward").Click();
        Assert.Equal(("feed", ReadState.ReadNext), (library.Moves.Last().Id, library.Moves.Last().Request.State));
    }

    [Fact]
    public void A_new_feed_post_can_be_ignored_from_its_card()
    {
        var page = Render<Articles>();

        page.Find("[data-article=feed] .trove-article-ignore").Click();

        Assert.Equal(("feed", ReadState.Ignored), (library.Moves.Last().Id, library.Moves.Last().Request.State));
        Assert.Empty(page.FindAll("[data-article=saved] .trove-article-ignore"));
    }

    [Fact]
    public void A_page_CORA_wont_read_says_why_and_offers_another_try()
    {
        library.Knowledge["saved"] = new CoraKnowledge(KnowledgeState.NotInCora);
        library.CoraFailure = new InvalidOperationException("CORA couldn't read this page. The site answered 403.");
        var page = Render<Articles>();

        page.Find("[data-article=saved] .trove-cora-add").Click();

        Assert.Contains("The site answered 403.", page.Find("[role=alert]").TextContent, StringComparison.Ordinal);
        Assert.Equal("Add to CORA", page.Find("[data-article=saved] .trove-cora-add").TextContent.Trim());

        library.Knowledge["saved"] = new CoraKnowledge(KnowledgeState.Failed, "c3", "failed");
        page = Render<Articles>();
        Assert.Equal("Try again", page.Find("[data-article=saved] .trove-cora-add").TextContent.Trim());
    }

    [Fact]
    public void A_brand_new_link_can_go_to_CORA_as_it_is_saved()
    {
        var page = Render<Articles>();

        page.Find("#article-url").Input("https://example.com/not-anywhere-yet");
        page.Find(".trove-add-cora input").Change(true);
        page.Find("form").Submit();

        Assert.True(library.Adds.Single().AddToCora);
        var status = page.Find("[role=status]").TextContent;
        Assert.Contains("Added to Read next. CORA has the page.", status, StringComparison.Ordinal);
    }

    [Fact]
    public void A_link_CORA_wont_take_is_still_saved_and_says_why()
    {
        library.AddCoraError = "CORA couldn't read this page. It isn't a web page.";
        var page = Render<Articles>();

        page.Find("#article-url").Input("https://example.com/file.zip");
        page.Find(".trove-add-cora input").Change(true);
        page.Find("form").Submit();

        Assert.Contains("Added to Read next. CORA couldn't read this page.", page.Find("[role=alert]").TextContent, StringComparison.Ordinal);
        Assert.NotNull(page.Find("[data-article=new]"));
    }

    [Fact]
    public void When_CORA_cant_be_asked_the_list_still_shows_and_says_why()
    {
        library.KnowledgeError = "CORA couldn't be reached to say which articles it has read.";
        var page = Render<Articles>();

        Assert.Contains("Couldn't check which articles are in CORA.", page.Markup, StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".trove-article-cora"));
        Assert.NotNull(page.Find("[data-article=saved]"));
    }

    private static Article Saved(string id, string url, string title, ReadState state, ArticleSource source = ArticleSource.Link, string? sourceName = null) =>
        new(id, url, title, new Uri(url).Host, null, null, null, source, sourceName, Added, state, null);

    private sealed class FakeLibrary : IArticleLibrary
    {
        private readonly List<Article> articles =
        [
            Saved("saved", "https://example.com/saved", "Saved earlier", ReadState.ReadNext),
            Saved("feed", "https://feed.example.com/post", "From a feed", ReadState.New, ArticleSource.Feed, "Example feed"),
        ];

        public string? LoadFailure { get; set; }

        public FeedSyncStatus? Feeds { get; set; }

        public Article? Extra { get; set; }

        public string? AddFailure { get; set; }

        public List<AddArticleRequest> Adds { get; } = [];

        public List<(string Id, MoveArticleRequest Request)> Moves { get; } = [];

        public List<string> Removes { get; } = [];

        public Dictionary<string, CoraKnowledge> Knowledge { get; } = [];

        public string? KnowledgeError { get; set; }

        public List<string> CoraAdds { get; } = [];

        public Exception? CoraFailure { get; set; }

        public string? AddCoraError { get; set; }

        private Article WithCora(Article a) => a with { Cora = Knowledge.GetValueOrDefault(a.Id) };

        public Task<ArticleBoard> GetBoardAsync(CancellationToken cancellationToken = default) =>
            LoadFailure is { } failure
                ? Task.FromException<ArticleBoard>(new HttpRequestException(failure))
                : Task.FromResult(new ArticleBoard(
                    [.. articles.Select(WithCora), .. Extra is null ? Array.Empty<Article>() : [Extra]], Feeds, KnowledgeError));

        public Task<AddArticleResult> AddAsync(AddArticleRequest request, CancellationToken cancellationToken = default)
        {
            Adds.Add(request);
            if (AddFailure is { } failure)
            {
                throw new ArgumentException(failure);
            }

            if (articles.FirstOrDefault(a => a.Url == request.Url) is { } existing)
            {
                return Task.FromResult(new AddArticleResult(existing, AlreadySaved: true));
            }

            var article = Saved("new", request.Url, request.Title ?? request.Url, ReadState.ReadNext);
            articles.Add(article);
            if (request.AddToCora && AddCoraError is { } coraError)
            {
                return Task.FromResult(new AddArticleResult(article, AlreadySaved: false, coraError));
            }

            return Task.FromResult(new AddArticleResult(
                request.AddToCora ? article with { Cora = new CoraKnowledge(KnowledgeState.Queued, "c9", "pending") } : article,
                AlreadySaved: false));
        }

        public Task<Article> MoveAsync(string id, MoveArticleRequest request, CancellationToken cancellationToken = default)
        {
            Moves.Add((id, request));
            var moved = articles.Single(a => a.Id == id) with
            {
                State = request.State,
                ReadOn = request.State == ReadState.Done ? request.Today : null,
            };
            articles[articles.FindIndex(a => a.Id == id)] = moved;
            return Task.FromResult(moved);
        }

        public Task<Article> AddToCoraAsync(string id, CancellationToken cancellationToken = default)
        {
            CoraAdds.Add(id);
            if (CoraFailure is { } failure)
            {
                return Task.FromException<Article>(failure);
            }

            Knowledge[id] = new CoraKnowledge(KnowledgeState.Queued, "c9", "unread");
            return Task.FromResult(WithCora(articles.Single(a => a.Id == id)));
        }

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            Removes.Add(id);
            articles.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }
    }
}
