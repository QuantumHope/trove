using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;
using ReactiveUI.Avalonia;

namespace Trove.Desktop.Tests;

public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseReactiveUI(_ => { });
}

/// <summary>Runs test bodies on one shared headless Avalonia session (the HopeLab Bench's harness).</summary>
public static class Ui
{
    public const string ThemeCollection = "Application theme";

    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() => HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder)));

    public static Task Run(Func<Task> body) => Session.Value.Dispatch(async () =>
    {
        await body();
        return true;
    }, CancellationToken.None);

    public static void Pump() => Dispatcher.UIThread.RunJobs();
}

[CollectionDefinition(Ui.ThemeCollection)]
public sealed class ThemeCollectionDefinition;
