using System.Reactive;
using ReactiveUI;
using Trove.Desktop.Services;

namespace Trove.Desktop.ViewModels;

/// <summary>Which of the window's faces is showing.</summary>
public enum ShellScreen
{
    /// <summary>Checking the saved address at start-up.</summary>
    Starting,

    /// <summary>The connect screen: first start, or "Change CORA address…" from Trove's Settings.</summary>
    Connect,

    /// <summary>Trove itself, in the web view.</summary>
    Trove,

    /// <summary>CORA can't be reached and this window has nothing saved to show.</summary>
    Offline,
}

/// <summary>
/// The window's whole flow. A saved address is checked at start: if CORA answers, Trove opens; if it doesn't and
/// Trove has opened here before, Trove still opens, since its service worker shows the saved board and says how old it
/// is; otherwise the offline screen explains. The web view reports back through the On… methods.
/// </summary>
public sealed class ShellViewModel : ReactiveObject
{
    private readonly DesktopSettings settings;
    private readonly ICoraProbe probe;
    private DesktopPreferences preferences;
    private ShellScreen screen = ShellScreen.Starting;
    private string addressText = "";
    private string? connectProblem;
    private bool isChecking;
    private string offlineTitle = "";
    private string offlineText = "";
    private Uri? trovePage;

    public ShellViewModel(DesktopSettings settings, ICoraProbe probe, string version)
    {
        this.settings = settings;
        this.probe = probe;
        Version = version;
        preferences = settings.Load();
        addressText = preferences.CoraAddress ?? "";

        var idle = this.WhenAnyValue(x => x.IsChecking, checking => !checking);
        ConnectCommand = ReactiveCommand.CreateFromTask(ConnectAsync, idle);
        CancelCommand = ReactiveCommand.Create(Cancel);
        RetryCommand = ReactiveCommand.CreateFromTask(StartAsync, idle);
        ChangeAddressCommand = ReactiveCommand.Create(ShowConnect);
    }

    public string Version { get; }

    public ShellScreen Screen
    {
        get => screen;
        private set
        {
            this.RaiseAndSetIfChanged(ref screen, value);
            this.RaisePropertyChanged(nameof(ShowsConnect));
            this.RaisePropertyChanged(nameof(ShowsTrove));
            this.RaisePropertyChanged(nameof(ShowsOffline));
            this.RaisePropertyChanged(nameof(ShowsStarting));
        }
    }

    public bool ShowsStarting => Screen == ShellScreen.Starting;
    public bool ShowsConnect => Screen == ShellScreen.Connect;
    public bool ShowsTrove => Screen == ShellScreen.Trove;
    public bool ShowsOffline => Screen == ShellScreen.Offline;

    /// <summary>The address field.</summary>
    public string AddressText
    {
        get => addressText;
        set
        {
            this.RaiseAndSetIfChanged(ref addressText, value);
            ConnectProblem = null;
        }
    }

    /// <summary>Why the last connect didn't work, under the field; null when there's nothing to say.</summary>
    public string? ConnectProblem
    {
        get => connectProblem;
        private set
        {
            this.RaiseAndSetIfChanged(ref connectProblem, value);
            this.RaisePropertyChanged(nameof(HasConnectProblem));
        }
    }

    public bool HasConnectProblem => ConnectProblem is not null;

    public bool IsChecking
    {
        get => isChecking;
        private set
        {
            this.RaiseAndSetIfChanged(ref isChecking, value);
            this.RaisePropertyChanged(nameof(ConnectLabel));
        }
    }

    public string ConnectLabel => IsChecking ? "Checking…" : "Connect";

    /// <summary>True when an address is already saved, so the connect screen is a change and can be cancelled.</summary>
    public bool CanCancel => preferences.CoraAddress is not null;

    /// <summary>The first line on the connect screen.</summary>
    public string ConnectTitle => CanCancel ? "Change CORA address" : "Connect to CORA";

    public string ConnectIntro => CanCancel
        ? "Trove opens from this CORA. Your boards, goals and settings stay in CORA, so another CORA has its own."
        : "Trove's boards live in your CORA. Type its address once and this window opens Trove from there every time.";

    public string OfflineTitle
    {
        get => offlineTitle;
        private set => this.RaiseAndSetIfChanged(ref offlineTitle, value);
    }

    public string OfflineText
    {
        get => offlineText;
        private set => this.RaiseAndSetIfChanged(ref offlineText, value);
    }

    /// <summary>The page the web view should show while <see cref="Screen"/> is Trove.</summary>
    public Uri? TrovePage
    {
        get => trovePage;
        private set => this.RaiseAndSetIfChanged(ref trovePage, value);
    }

    /// <summary>The saved address, for the offline screen and the web view's checks.</summary>
    public string? CoraOrigin => preferences.CoraAddress;

    /// <summary>The theme the page last reported, for the window's own screens.</summary>
    public string Theme => preferences.Theme;

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public ReactiveCommand<Unit, Unit> RetryCommand { get; }

    public ReactiveCommand<Unit, Unit> ChangeAddressCommand { get; }

    /// <summary>The start-up check, and Try again on the offline screen.</summary>
    public async Task StartAsync()
    {
        if (preferences.CoraAddress is not { } origin)
        {
            ShowConnect();
            return;
        }

        Screen = ShellScreen.Starting;
        var result = await probe.CheckAsync(origin);
        switch (result)
        {
            case ProbeResult.Ready:
            case ProbeResult.Unreachable when preferences.OpenedTrove:
                OpenTrove(origin);
                break;
            case ProbeResult.Unreachable:
                ShowOffline(
                    "CORA can't be reached",
                    $"This window hasn't opened Trove from {Host(origin)} yet, so it has no saved copy to show. Check that this computer is on your tailnet, then try again.");
                break;
            default:
                ShowOffline(
                    "Trove isn't at this address",
                    $"{Host(origin)} answered, but it doesn't serve Trove. Check that the Trove module is on in CORA, or change the address.");
                break;
        }
    }

    private async Task ConnectAsync()
    {
        if (!CoraAddress.TryParse(AddressText, out var origin, out var problem))
        {
            ConnectProblem = problem;
            return;
        }

        IsChecking = true;
        try
        {
            switch (await probe.CheckAsync(origin))
            {
                case ProbeResult.Ready:
                    var changed = !string.Equals(origin, preferences.CoraAddress, StringComparison.OrdinalIgnoreCase);
                    // A new CORA starts with no saved copy; the old one's is still in the web view's storage.
                    preferences = preferences with { CoraAddress = origin, OpenedTrove = !changed && preferences.OpenedTrove };
                    await SaveAsync();
                    AddressText = origin;
                    RaiseConnectText();
                    OpenTrove(origin);
                    break;
                case ProbeResult.NoTrove:
                    ConnectProblem = $"{Host(origin)} answered, but it doesn't serve Trove. Check the address, and that the Trove module is on in CORA.";
                    break;
                default:
                    ConnectProblem = $"Couldn't reach {Host(origin)}. Check the address, and that this computer is on your tailnet.";
                    break;
            }
        }
        finally
        {
            IsChecking = false;
        }
    }

    private void Cancel()
    {
        if (preferences.CoraAddress is { } origin)
        {
            AddressText = origin;
            OpenTrove(origin);
        }
    }

    private void ShowConnect()
    {
        AddressText = preferences.CoraAddress ?? "";
        ConnectProblem = null;
        RaiseConnectText();
        Screen = ShellScreen.Connect;
    }

    private void OpenTrove(string origin)
    {
        TrovePage = CoraAddress.TrovePage(origin);
        Screen = ShellScreen.Trove;
    }

    private void ShowOffline(string title, string text)
    {
        OfflineTitle = title;
        OfflineText = text;
        Screen = ShellScreen.Offline;
    }

    /// <summary>The web view is about to load a page; true means the window took it and the load should stop.</summary>
    public bool OnNavigating(Uri? uri)
    {
        if (!ShellRoutes.IsChangeAddress(uri))
        {
            return false;
        }

        ShowConnect();
        return true;
    }

    /// <summary>A load finished. Trove loading means a saved copy exists from now on; Trove failing to load, offline.</summary>
    public async Task OnNavigatedAsync(Uri? uri, bool succeeded)
    {
        if (preferences.CoraAddress is not { } origin || !ShellRoutes.IsTrove(uri, origin) || Screen != ShellScreen.Trove)
        {
            return;
        }

        if (succeeded)
        {
            if (!preferences.OpenedTrove)
            {
                preferences = preferences with { OpenedTrove = true };
                await SaveAsync();
            }

            return;
        }

        ShowOffline(
            "CORA can't be reached",
            $"Trove couldn't load from {Host(origin)}, and this window has no saved copy of it. Check that this computer is on your tailnet, then try again.");
    }

    /// <summary>The page's theme choice, read after each load, so the window's own screens match it next time.</summary>
    public async Task OnThemeReportedAsync(string? theme)
    {
        if (string.IsNullOrWhiteSpace(theme) || theme == preferences.Theme)
        {
            return;
        }

        preferences = preferences with { Theme = theme };
        this.RaisePropertyChanged(nameof(Theme));
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await settings.SaveAsync(preferences);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The window still works this run; it just asks again next start.
        }
    }

    private void RaiseConnectText()
    {
        this.RaisePropertyChanged(nameof(CanCancel));
        this.RaisePropertyChanged(nameof(ConnectTitle));
        this.RaisePropertyChanged(nameof(ConnectIntro));
        this.RaisePropertyChanged(nameof(CoraOrigin));
    }

    private static string Host(string origin) => new Uri(origin).Authority;
}
