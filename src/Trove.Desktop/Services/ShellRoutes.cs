namespace Trove.Desktop.Services;

/// <summary>
/// The addresses the window treats as its own. Trove's Settings page links to <see cref="ChangeAddress"/> when it's
/// inside the window (Trove.UI's DesktopShell.ChangeAddressPath); the window catches that link and shows its connect
/// screen instead of loading it.
/// </summary>
public static class ShellRoutes
{
    public const string ChangeAddress = "/trove/desktop/change-cora-address";

    /// <summary>The product token the window adds to its user agent, so the page knows it's inside the window.</summary>
    public static string UserAgentToken(string version) => $"TroveDesktop/{version}";

    public static bool IsChangeAddress(Uri? uri) =>
        uri is { IsAbsoluteUri: true } && uri.AbsolutePath.TrimEnd('/').Equals(ChangeAddress, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a page is Trove itself on this CORA, the page a failed load should be judged by.</summary>
    public static bool IsTrove(Uri? uri, string origin) =>
        uri is { IsAbsoluteUri: true }
        && uri.GetLeftPart(UriPartial.Authority).Equals(origin, StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.StartsWith("/trove", StringComparison.OrdinalIgnoreCase);

    /// <summary>Adds the token once to a user agent the web view reports.</summary>
    public static string WithToken(string? userAgent, string version)
    {
        var token = UserAgentToken(version);
        var baseAgent = userAgent?.Trim() ?? "";
        return baseAgent.Contains("TroveDesktop/", StringComparison.Ordinal) ? baseAgent
            : baseAgent.Length == 0 ? token
            : $"{baseAgent} {token}";
    }
}
