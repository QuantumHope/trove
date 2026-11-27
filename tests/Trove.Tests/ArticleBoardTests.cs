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

        Assert.Equal(new AddArticleRequest("https://example.com/new", "A new one"), library.Adds.Single());
        Assert.Contains("Saved to Read next", page.Find("[role=status]").TextContent, StringComparison.Ordinal);
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

        public string? AddFailure { get; set; }

        public List<AddArticleRequest> Adds { get; } = [];

        public List<(string Id, MoveArticleRequest Request)> Moves { get; } = [];

        public List<string> Removes { get; } = [];

        public Task<ArticleBoard> GetBoardAsync(CancellationToken cancellationToken = default) =>
            LoadFailure is { } failure
                ? Task.FromException<ArticleBoard>(new HttpRequestException(failure))
                : Task.FromResult(new ArticleBoard([.. articles]));

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
            return Task.FromResult(new AddArticleResult(article, AlreadySaved: false));
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

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        {
            Removes.Add(id);
            articles.RemoveAll(a => a.Id == id);
            return Task.CompletedTask;
        }
    }
}
