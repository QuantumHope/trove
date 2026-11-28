using System.Reactive.Linq;
using Avalonia;
using Avalonia.Headless;
using Trove.Desktop.Services;
using Trove.Desktop.ViewModels;
using Trove.Desktop.Views;

namespace Trove.Desktop.Tests;

/// <summary>
/// Renders the window's own screens headlessly and saves PNGs, so they can be looked at without running the app. The
/// PNGs go to <c>TROVE_SCREENSHOTS</c> when it's set, otherwise to <c>screenshots/</c> beside the test assembly. Trove's
/// page itself isn't drawn here: the web view needs a real platform.
/// </summary>
[Collection(Ui.ThemeCollection)]
public sealed class RenderTests
{
    private const string Cora = "https://cora.example.ts.net";

    private static string ShotPath(string name)
    {
        var folder = Environment.GetEnvironmentVariable("TROVE_SCREENSHOTS") is { Length: > 0 } env
            ? env
            : Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, name);
    }

    private static async Task<string> RenderAsync(string name, TempHome temp, FakeProbe probe, string theme, Func<ShellViewModel, Task>? act = null)
    {
        WindowTheme.Apply(Application.Current!, theme);
        var shell = new ShellViewModel(temp.Settings, probe, "0.1.0");
        var window = new MainWindow(temp.Home) { Width = 1100, Height = 720, DataContext = shell };
        window.Show();
        for (var i = 0; i < 4; i++)
        {
            Ui.Pump();
            await Task.Delay(10);
        }

        if (act is not null)
        {
            await act(shell);
        }

        for (var i = 0; i < 6; i++)
        {
            Ui.Pump();
            await Task.Delay(10);
        }

        var path = ShotPath(name);
        using (var frame = window.CaptureRenderedFrame())
        {
            Assert.NotNull(frame);
            frame.Save(path);
        }

        window.Close();
        return path;
    }

    [Theory]
    [InlineData("dark")]
    [InlineData("light")]
    public Task First_start_connect_screen_renders(string theme) => Ui.Run(async () =>
    {
        using var temp = new TempHome();
        var path = await RenderAsync($"connect-{theme}.png", temp, new FakeProbe(), theme);
        Assert.True(new FileInfo(path).Length > 5_000);
    });

    [Fact]
    public Task Connect_screen_with_a_problem_renders() => Ui.Run(async () =>
    {
        using var temp = new TempHome();
        var path = await RenderAsync("connect-unreachable.png", temp, new FakeProbe(ProbeResult.Unreachable), "dark", async shell =>
        {
            shell.AddressText = "cora.example.ts.net";
            await shell.ConnectCommand.Execute();
        });
        Assert.True(new FileInfo(path).Length > 5_000);
    });

    [Fact]
    public Task Change_address_screen_renders() => Ui.Run(async () =>
    {
        using var temp = new TempHome();
        // Offline with no saved copy, so the web view (which needs a real platform) is never made.
        await temp.Settings.SaveAsync(new DesktopPreferences(Cora));
        var path = await RenderAsync("change-address.png", temp, new FakeProbe(ProbeResult.Unreachable), "dark", shell =>
        {
            shell.ChangeAddressCommand.Execute().Subscribe();
            return Task.CompletedTask;
        });
        Assert.True(new FileInfo(path).Length > 5_000);
    });

    [Fact]
    public Task Offline_screen_renders() => Ui.Run(async () =>
    {
        using var temp = new TempHome();
        await temp.Settings.SaveAsync(new DesktopPreferences(Cora));
        var path = await RenderAsync("offline.png", temp, new FakeProbe(ProbeResult.Unreachable), "dark");
        Assert.True(new FileInfo(path).Length > 5_000);
    });
}
