namespace Trove.Desktop.Services;

/// <summary>
/// Turns what someone types into CORA's origin. "cora.example.ts.net" becomes https://cora.example.ts.net; a pasted
/// link to Trove or any CORA page keeps only its origin, since the window always opens {origin}/trove/.
/// </summary>
public static class CoraAddress
{
    public const string Empty = "Type CORA's address, such as https://cora.your-tailnet.ts.net.";
    public const string NotWeb = "That isn't a web address. CORA's starts with https:// or http://.";
    public const string WithSignIn = "Leave the name and password out of the address. CORA signs you in itself.";

    /// <summary>The origin, or false with a sentence saying what's wrong.</summary>
    public static bool TryParse(string? typed, out string origin, out string problem)
    {
        origin = "";
        problem = "";
        var text = typed?.Trim() ?? "";
        if (text.Length == 0)
        {
            problem = Empty;
            return false;
        }

        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || string.IsNullOrEmpty(uri.Host))
        {
            problem = NotWeb;
            return false;
        }

        if (uri.UserInfo.Length > 0)
        {
            problem = WithSignIn;
            return false;
        }

        origin = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }

    /// <summary>Trove's page on that CORA.</summary>
    public static Uri TrovePage(string origin) => new(origin.TrimEnd('/') + "/trove/");
}
