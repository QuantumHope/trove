using System.Reactive.Linq;
using Trove.Desktop.Services;
using Trove.Desktop.ViewModels;

namespace Trove.Desktop.Tests;

public sealed class ShellTests : IDisposable
{
    private const string Cora = "https://cora.example.ts.net";
    private readonly TempHome temp = new();
    private readonly FakeProbe probe = new();

    public void Dispose() => temp.Dispose();

    private ShellViewModel Shell() => new(temp.Settings, probe, "0.1.0");

    private Task Saved(DesktopPreferences preferences) => temp.Settings.SaveAsync(preferences);

    [Fact]
    public Task First_start_asks_for_coras_address_with_nothing_to_cancel() => Ui.Run(async () =>
    {
        var shell = Shell();

        await shell.StartAsync();

        Assert.Equal(ShellScreen.Connect, shell.Screen);
        Assert.False(shell.CanCancel);
        Assert.Equal("Connect to CORA", shell.ConnectTitle);
        Assert.Empty(probe.Checked);
    });

    [Fact]
    public Task Connecting_checks_the_address_saves_it_and_opens_trove() => Ui.Run(async () =>
    {
        var shell = Shell();
        await shell.StartAsync();

        shell.AddressText = "cora.example.ts.net";
        await shell.ConnectCommand.Execute();

        Assert.Equal([Cora], probe.Checked);
        Assert.Equal(ShellScreen.Trove, shell.Screen);
        Assert.Equal(new Uri(Cora + "/trove/"), shell.TrovePage);
        Assert.Equal(Cora, temp.Settings.Load().CoraAddress);
    });

    [Fact]
    public Task A_typo_is_caught_before_anything_is_checked() => Ui.Run(async () =>
    {
        var shell = Shell();
        await shell.StartAsync();

        shell.AddressText = "ftp://cora";
        await shell.ConnectCommand.Execute();

        Assert.Equal(CoraAddress.NotWeb, shell.ConnectProblem);
        Assert.Empty(probe.Checked);

        shell.AddressText = "cora.example.ts.net";
        Assert.Null(shell.ConnectProblem);
    });

    [Theory]
    [InlineData(ProbeResult.Unreachable, "Couldn't reach cora.example.ts.net. Check the address, and that this computer is on your tailnet.")]
    [InlineData(ProbeResult.NoTrove, "cora.example.ts.net answered, but it doesn't serve Trove. Check the address, and that the Trove module is on in CORA.")]
    public Task An_address_that_doesnt_work_stays_on_the_connect_screen_and_says_why(ProbeResult result, string problem) => Ui.Run(async () =>
    {
        probe.Result = result;
        var shell = Shell();
        await shell.StartAsync();

        shell.AddressText = Cora;
        await shell.ConnectCommand.Execute();

        Assert.Equal(ShellScreen.Connect, shell.Screen);
        Assert.Equal(problem, shell.ConnectProblem);
        Assert.Null(temp.Settings.Load().CoraAddress);
    });

    [Fact]
    public Task A_saved_address_opens_trove_straight_away() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora));
        var shell = Shell();

        await shell.StartAsync();

        Assert.Equal(ShellScreen.Trove, shell.Screen);
    });

    [Fact]
    public Task Offline_with_a_saved_copy_still_opens_trove_for_the_service_worker_to_answer() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora, OpenedTrove: true));
        probe.Result = ProbeResult.Unreachable;
        var shell = Shell();

        await shell.StartAsync();

        Assert.Equal(ShellScreen.Trove, shell.Screen);
    });

    [Fact]
    public Task Offline_before_trove_ever_opened_here_explains_and_can_try_again() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora));
        probe.Result = ProbeResult.Unreachable;
        var shell = Shell();

        await shell.StartAsync();

        Assert.Equal(ShellScreen.Offline, shell.Screen);
        Assert.Equal("CORA can't be reached", shell.OfflineTitle);
        Assert.Contains("hasn't opened Trove from cora.example.ts.net yet", shell.OfflineText);

        probe.Result = ProbeResult.Ready;
        await shell.RetryCommand.Execute();

        Assert.Equal(ShellScreen.Trove, shell.Screen);
    });

    [Fact]
    public Task Trove_loading_once_means_a_saved_copy_from_then_on() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora));
        var shell = Shell();
        await shell.StartAsync();

        await shell.OnNavigatedAsync(new Uri(Cora + "/trove/"), succeeded: true);

        Assert.True(temp.Settings.Load().OpenedTrove);
    });

    [Fact]
    public Task Trove_failing_to_load_shows_the_offline_screen() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora, OpenedTrove: true));
        var shell = Shell();
        await shell.StartAsync();

        await shell.OnNavigatedAsync(new Uri(Cora + "/trove/"), succeeded: false);

        Assert.Equal(ShellScreen.Offline, shell.Screen);
    });

    [Fact]
    public Task A_frame_failing_to_load_leaves_trove_alone() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora, OpenedTrove: true));
        var shell = Shell();
        await shell.StartAsync();

        await shell.OnNavigatedAsync(new Uri("https://blog.example.com/post"), succeeded: false);

        Assert.Equal(ShellScreen.Trove, shell.Screen);
    });

    [Fact]
    public Task Change_address_from_troves_settings_opens_the_connect_screen_and_cancel_goes_back() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora, OpenedTrove: true));
        var shell = Shell();
        await shell.StartAsync();

        Assert.False(shell.OnNavigating(new Uri(Cora + "/trove/settings")));
        Assert.True(shell.OnNavigating(new Uri(Cora + ShellRoutes.ChangeAddress)));

        Assert.Equal(ShellScreen.Connect, shell.Screen);
        Assert.True(shell.CanCancel);
        Assert.Equal("Change CORA address", shell.ConnectTitle);
        Assert.Equal(Cora, shell.AddressText);

        await shell.CancelCommand.Execute();

        Assert.Equal(ShellScreen.Trove, shell.Screen);
    });

    [Fact]
    public Task A_new_cora_starts_without_a_saved_copy() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora, OpenedTrove: true));
        var shell = Shell();
        await shell.StartAsync();
        shell.OnNavigating(new Uri(Cora + ShellRoutes.ChangeAddress));

        shell.AddressText = "https://other.example.ts.net";
        await shell.ConnectCommand.Execute();

        Assert.Equal(new DesktopPreferences("https://other.example.ts.net"), temp.Settings.Load());
    });

    [Fact]
    public Task The_pages_theme_is_remembered_for_the_windows_own_screens() => Ui.Run(async () =>
    {
        await Saved(new DesktopPreferences(Cora));
        var shell = Shell();

        await shell.OnThemeReportedAsync("wopr");

        Assert.Equal("wopr", shell.Theme);
        Assert.Equal("wopr", temp.Settings.Load().Theme);
    });

    [Theory]
    [InlineData("wopr", true, "wopr")]
    [InlineData("system", true, "dark")]
    [InlineData("system", false, "light")]
    [InlineData("no-such-theme", false, "light")]
    public void System_follows_the_computer_and_unknown_themes_fall_back(string preference, bool systemDark, string id) =>
        Assert.Equal(id, WindowTheme.Resolve(preference, systemDark).Id);
}
