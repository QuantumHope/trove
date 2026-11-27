using System.Text.Json.Serialization;

namespace Trove.Core.Articles;

/// <summary>Where an article came from: a link you saved, or a feed CORA reads.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ArticleSource>))]
public enum ArticleSource
{
    Link,
    Feed,
}

/// <summary>An article on the reading list. The module keeps one per address per account.</summary>
/// <param name="Id">A short hash of the address, stable for the same link.</param>
/// <param name="Site">The host without <c>www.</c>, for a line under the title.</param>
/// <param name="SourceName">The feed's name, for feed articles.</param>
/// <param name="ReadOn">The day you finished it, on your own calendar.</param>
/// <param name="Cora">Whether CORA has read it; null when CORA couldn't be asked.</param>
public sealed record Article(
    string Id,
    string Url,
    string Title,
    string Site,
    string? Author,
    DateTimeOffset? PublishedAt,
    string? Summary,
    ArticleSource Source,
    string? SourceName,
    DateTimeOffset AddedAt,
    ReadState State,
    DateOnly? ReadOn,
    CoraKnowledge? Cora = null)
{
    /// <summary>Only links you saved can be removed; a feed article would come back on the next read, so it's ignored instead.</summary>
    public bool CanRemove => Source == ArticleSource.Link;
}

/// <summary>
/// The reading list, and how its copy of CORA's feed posts stands. Feeds are CORA's: each one there is set to send
/// its posts to Knowledge, Trove or both, and the ones sent to Trove arrive here in New.
/// </summary>
/// <param name="KnowledgeError">Why no article says whether CORA has read it, when CORA couldn't be asked.</param>
public sealed record ArticleBoard(IReadOnlyList<Article> Articles, FeedSyncStatus? Feeds = null, string? KnowledgeError = null);

/// <summary>Whether feed posts are being copied (<paramref name="Connected"/>), when it last tried, and why the last try failed.</summary>
public sealed record FeedSyncStatus(bool Connected, DateTimeOffset? LastSyncedAt, string? Error);

/// <summary>A link to save. The title is optional; without one the address stands in. <paramref name="AddToCora"/> also asks CORA to read it.</summary>
public sealed record AddArticleRequest(string Url, string? Title, bool AddToCora = false);

/// <param name="AlreadySaved">The link was on the list already; a new or ignored one moves back to Read next.</param>
/// <param name="CoraError">Why CORA didn't take the page, when it was asked to and couldn't. The link is saved either way.</param>
public sealed record AddArticleResult(Article Article, bool AlreadySaved, string? CoraError = null);

/// <summary>A move to another column. <paramref name="Today"/> is the person's own date, recorded when the move is to Done.</summary>
public sealed record MoveArticleRequest(ReadState State, DateOnly? Today = null);
