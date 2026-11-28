using System.ComponentModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Trove.Desktop.Services;
using Trove.Desktop.ViewModels;

namespace Trove.Desktop.Views;

/// <summary>
/// Holds the one web view and passes what it does to <see cref="ShellViewModel"/>. The view model decides; this file
/// only wires the platform's web view to it: where it keeps its storage, the user agent token Trove looks for, the
/// change-address link, failed loads, new-window links (to the system browser) and the page's theme.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Reads Trove's theme choice; "" when the page isn't Trove.</summary>
    private const string ThemeScript = "(window.trove && window.trove.theme) ? window.trove.theme.get() : ''";

    private readonly TroveHome home;
    private NativeWebView? webView;
    private bool adapterReady;

    public MainWindow() : this(TroveHome.ForUser())
    {
    }

    public MainWindow(TroveHome home)
    {
        this.home = home;
        InitializeComponent();
        DataContextChanged += (_, _) => Attach();
    }

    private ShellViewModel? Shell => DataContext as ShellViewModel;

    private void Attach()
    {
        if (Shell is not { } shell)
        {
            return;
        }

        shell.PropertyChanged += OnShellChanged;
        _ = shell.StartAsync();
    }

    private void OnShellChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Shell is not { } shell)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(ShellViewModel.Screen) when shell.ShowsTrove:
            case nameof(ShellViewModel.TrovePage) when shell.ShowsTrove:
                ShowTrove(shell.TrovePage);
                break;
            case nameof(ShellViewModel.Screen) when shell.ShowsConnect:
                Address.Focus();
                Address.SelectAll();
                break;
            case nameof(ShellViewModel.Theme):
                if (Application.Current is { } app)
                {
                    WindowTheme.Apply(app, shell.Theme);
                }

                break;
        }
    }

    private void ShowTrove(Uri? page)
    {
        if (page is null)
        {
            return;
        }

        if (webView is null)
        {
            webView = CreateWebView();
            WebHost.Children.Add(webView);
        }

        if (adapterReady)
        {
            webView.Navigate(page);
        }
    }

    private NativeWebView CreateWebView()
    {
        var view = new NativeWebView();
        Directory.CreateDirectory(home.WebViewData);
        var version = DesktopVersion.Current;

        view.EnvironmentRequested += (_, e) =>
        {
            // Trove's saved board lives in this storage, so it's kept in Trove's folder, not a temporary one.
            switch (e)
            {
                case WindowsWebView2EnvironmentRequestedEventArgs w:
                    w.UserDataFolder = home.WebViewData;
                    break;
                case GtkWebViewEnvironmentRequestedEventArgs g:
                    g.BaseDataDirectory = Path.Combine(home.WebViewData, "data");
                    g.BaseCacheDirectory = Path.Combine(home.WebViewData, "cache");
                    g.ApplicationNameForUserAgent = ShellRoutes.UserAgentToken(version);
                    break;
                case LinuxWpeWebViewEnvironmentRequestedEventArgs l:
                    l.DataDirectory = Path.Combine(home.WebViewData, "data");
                    l.CacheDirectory = Path.Combine(home.WebViewData, "cache");
                    break;
                case AppleWKWebViewEnvironmentRequestedEventArgs a:
                    a.ApplicationNameForUserAgent = ShellRoutes.UserAgentToken(version);
                    break;
            }
        };

        view.AdapterCreated += (_, _) =>
        {
            view.UserAgent = ShellRoutes.WithToken(view.UserAgent, version);
            adapterReady = true;
            ShowTrove(Shell?.TrovePage);
        };

        view.NavigationStarted += (_, e) =>
        {
            if (Shell?.OnNavigating(e.Request) == true)
            {
                e.Cancel = true;
            }
        };

        view.NavigationCompleted += async (_, e) =>
        {
            if (Shell is not { } shell)
            {
                return;
            }

            await shell.OnNavigatedAsync(e.Request, e.IsSuccess);
            if (e.IsSuccess)
            {
                await shell.OnThemeReportedAsync(await ReadThemeAsync(view));
            }
        };

        // Links that open a new tab (YouTube, the original article) go to the computer's own browser.
        view.NewWindowRequested += async (_, e) =>
        {
            e.Handled = true;
            if (e.Request is { Scheme: "https" or "http" } link)
            {
                await Launcher.LaunchUriAsync(link);
            }
        };

        return view;
    }

    private static async Task<string?> ReadThemeAsync(NativeWebView view)
    {
        try
        {
            var raw = await view.InvokeScript(ThemeScript);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            // Some engines hand back the value JSON-encoded ("\"dark\""), others as it is.
            return raw.TrimStart().StartsWith('"') ? JsonSerializer.Deserialize<string>(raw) : raw;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}
