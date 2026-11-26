namespace Trove.Core.Playthroughs;

/// <summary>The playthrough board's data. The server reads the database; the browser calls the server's API.</summary>
public interface IPlaythroughLibrary
{
    Task<PlaythroughBoard> GetBoardAsync(CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No such video.</exception>
    /// <exception cref="ArgumentException">The state or rating isn't allowed.</exception>
    Task<Playthrough> MoveAsync(string videoId, MoveRequest request, CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No such video.</exception>
    /// <exception cref="InvalidOperationException">The video isn't in progress or done.</exception>
    Task<Playthrough> RateAsync(string videoId, decimal? rating, CancellationToken cancellationToken = default);

    /// <summary>Reads new uploads from every configured channel now.</summary>
    Task<SyncStatus> SyncAsync(CancellationToken cancellationToken = default);
}
