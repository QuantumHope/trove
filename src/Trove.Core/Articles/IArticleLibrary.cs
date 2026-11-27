namespace Trove.Core.Articles;

/// <summary>The reading list's data, kept by CORA's Trove module.</summary>
public interface IArticleLibrary
{
    Task<ArticleBoard> GetBoardAsync(CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentException">The link isn't a web address.</exception>
    Task<AddArticleResult> AddAsync(AddArticleRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No such article.</exception>
    Task<Article> MoveAsync(string id, MoveArticleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Asks CORA to read the article's page, as Import knowledge in CORA would.</summary>
    /// <exception cref="KeyNotFoundException">No such article.</exception>
    /// <exception cref="InvalidOperationException">CORA couldn't read the page; the message says why.</exception>
    /// <exception cref="HttpRequestException">CORA couldn't be reached.</exception>
    Task<Article> AddToCoraAsync(string id, CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No such article.</exception>
    /// <exception cref="InvalidOperationException">The article came from a feed.</exception>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
