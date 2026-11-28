using Microsoft.JSInterop;

namespace Trove.UI.Desktop;

/// <summary>What the Trove desktop window says about itself: its version and the CORA address it opened.</summary>
public sealed record DesktopInfo(string Version, string Origin);

/// <summary>
/// Tells the pages whether they're showing inside the Trove desktop window, and asks the window to change where it
/// connects. The window watches its own navigation for <see cref="ChangeAddressPath"/> and opens its connect screen in
/// place of the page, so this works without any messaging bridge.
/// </summary>
public sealed class DesktopShell(IJSRuntime js)
{
    /// <summary>The path the window intercepts. Kept in step with Trove.Desktop's ShellRoutes.</summary>
    public const string ChangeAddressPath = "desktop/change-cora-address";

    private DesktopInfo? info;
    private bool checkedOnce;

    /// <summary>The window's details, or null in an ordinary browser.</summary>
    public async Task<DesktopInfo?> GetAsync()
    {
        if (!checkedOnce)
        {
            try
            {
                await using var module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/Trove.UI/desktop.js");
                info = await module.InvokeAsync<DesktopInfo?>("shell");
            }
            catch (JSException)
            {
                // The script couldn't load (offline with no saved copy of it): treat it as an ordinary browser.
                info = null;
            }

            checkedOnce = true;
        }

        return info;
    }
}
