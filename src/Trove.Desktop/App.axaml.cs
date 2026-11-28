using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Trove.Desktop.Services;
using Trove.Desktop.ViewModels;
using Trove.Desktop.Views;

namespace Trove.Desktop;

public sealed class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        WindowTheme.Apply(this, null);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var home = TroveHome.ForUser();
            var shell = new ShellViewModel(new DesktopSettings(home.SettingsFile), new CoraProbe(new HttpClient()), DesktopVersion.Current);
            WindowTheme.Apply(this, shell.Theme);
            desktop.MainWindow = new MainWindow(home) { DataContext = shell };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
