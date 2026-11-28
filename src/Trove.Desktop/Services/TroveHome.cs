namespace Trove.Desktop.Services;

/// <summary>
/// Where the window keeps its files: <c>~/.trove</c>, or the folder in <c>TROVE_HOME</c>. It holds desktop.json (the
/// CORA address and theme) and webview/, the web view's own storage, where Trove's saved copy of the board lives.
/// </summary>
public sealed class TroveHome(string root)
{
    /// <summary>The variable that moves the folder, as HopeLab's other apps allow (handy for a second profile or tests).</summary>
    public const string Variable = "TROVE_HOME";

    public string Root { get; } = Path.GetFullPath(root);

    public string SettingsFile => Path.Combine(Root, "desktop.json");

    public string WebViewData => Path.Combine(Root, "webview");

    public static TroveHome ForUser()
    {
        var overridden = Environment.GetEnvironmentVariable(Variable);
        return new TroveHome(string.IsNullOrWhiteSpace(overridden)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".trove")
            : overridden);
    }
}
