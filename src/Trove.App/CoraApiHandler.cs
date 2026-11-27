using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;
using Trove.UI;

namespace Trove.App;

/// <summary>
/// Every call to CORA goes through here. CORA's cookie answers 401 (not a sign-in redirect) only when the request
/// says it is an API call, and CORA turns away writes without X-Requested-With, so both headers go on every request.
/// A 401 means the CORA sign-in has lapsed, so the page goes to CORA's sign-in and comes back here afterwards. A 502
/// says which side couldn't be reached; see <see cref="CoraProblems.GatewayReason"/>.
/// </summary>
public sealed class CoraApiHandler(NavigationManager navigation) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        var response = await base.SendAsync(request, cancellationToken);
        switch (response.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                navigation.NavigateTo(SignInUrl(navigation.Uri), forceLoad: true);
                throw new HttpRequestException("Signing you in to CORA…", null, response.StatusCode);
            case HttpStatusCode.Conflict:
                throw new HttpRequestException("The Trove module is turned off in CORA (Admin → Modules).", null, response.StatusCode);
            case HttpStatusCode.BadGateway:
                throw new HttpRequestException(
                    CoraProblems.GatewayReason(await response.Content.ReadAsStringAsync(cancellationToken))
                        ?? "CORA couldn't reach the Trove module.",
                    null, response.StatusCode);
            default:
                return response;
        }
    }

    /// <summary>CORA's sign-in takes a relative return path, so it brings you back to the same Trove page.</summary>
    public static string SignInUrl(string currentUri) =>
        "/signin?returnUrl=" + Uri.EscapeDataString(new Uri(currentUri).PathAndQuery);
}
