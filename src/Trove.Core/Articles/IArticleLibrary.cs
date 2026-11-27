namespace Trove.Core.Articles;

/// <summary>The reading list's data, kept by CORA's Trove module.</summary>
public interface IArticleLibrary
{
    Task<ArticleBoard> GetBoardAsync(CancellationToken cancellationToken = default);

    /// <exception cref="ArgumentException">The link isn't a web address.</exception>
    Task<AddArticleResult> AddAsync(AddArticleRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No such article.</exception>
    Task<Article> MoveAsync(string id, MoveArticleRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No such article.</exception>
    /// <exception cref="InvalidOperationException">The article came from a feed.</exception>
    Task RemoveAsync(string id, CancellationToken cancellationToken = default);
}
