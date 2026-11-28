namespace Trove.UI.Connection;

/// <summary>
/// Sits under the CORA handler and tells <see cref="ConnectionStatus"/> what each answer was: CORA itself, or the
/// service worker's saved copy. A call that reaches nothing (no status code at all) marks Trove offline and says so in
/// words a person can act on, since the browser's own message is "TypeError: Failed to fetch".
/// </summary>
public sealed class ConnectionHandler(ConnectionStatus status) : DelegatingHandler
{
    /// <summary>After a write: the pages put "That change didn't save." in front of it.</summary>
    public const string ChangeNotSaved = "CORA can't be reached right now. Try again once you're back online.";

    /// <summary>After a read with no saved copy to answer it.</summary>
    public const string NothingSaved = "CORA can't be reached right now, and this device has no saved copy of this yet.";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null && !cancellationToken.IsCancellationRequested)
        {
            status.Unreachable();
            throw new HttpRequestException(request.Method == HttpMethod.Get ? NothingSaved : ChangeNotSaved, ex);
        }

        status.Observe(response);
        return response;
    }
}
