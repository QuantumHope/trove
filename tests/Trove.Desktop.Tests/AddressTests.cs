using Trove.Desktop.Services;

namespace Trove.Desktop.Tests;

public sealed class AddressTests
{
    [Theory]
    [InlineData("cora.example.ts.net", "https://cora.example.ts.net")]
    [InlineData("  https://cora.example.ts.net/  ", "https://cora.example.ts.net")]
    [InlineData("https://cora.example.ts.net/trove/playthroughs?x=1", "https://cora.example.ts.net")]
    [InlineData("http://localhost:5173", "http://localhost:5173")]
    [InlineData("HTTPS://CORA.example.ts.net:8443/admin", "https://cora.example.ts.net:8443")]
    public void Typed_addresses_become_coras_origin(string typed, string origin)
    {
        Assert.True(CoraAddress.TryParse(typed, out var parsed, out _));
        Assert.Equal(origin, parsed);
    }

    [Theory]
    [InlineData("", CoraAddress.Empty)]
    [InlineData("   ", CoraAddress.Empty)]
    [InlineData("ftp://cora.example", CoraAddress.NotWeb)]
    [InlineData("file:///home/me", CoraAddress.NotWeb)]
    [InlineData("https://", CoraAddress.NotWeb)]
    [InlineData("https://me:secret@cora.example.ts.net", CoraAddress.WithSignIn)]
    public void Addresses_that_wont_work_say_why(string typed, string problem)
    {
        Assert.False(CoraAddress.TryParse(typed, out _, out var said));
        Assert.Equal(problem, said);
    }

    [Fact]
    public void Trove_opens_at_trove_on_that_cora() =>
        Assert.Equal("https://cora.example.ts.net/trove/", CoraAddress.TrovePage("https://cora.example.ts.net").ToString());

    [Theory]
    [InlineData("https://cora.example.ts.net/trove/desktop/change-cora-address", true)]
    [InlineData("https://cora.example.ts.net/trove/desktop/change-cora-address/", true)]
    [InlineData("https://cora.example.ts.net/trove/settings", false)]
    [InlineData("https://www.youtube.com/embed/abc", false)]
    public void The_change_address_link_is_the_windows_own(string uri, bool caught) =>
        Assert.Equal(caught, ShellRoutes.IsChangeAddress(new Uri(uri)));

    [Fact]
    public void The_path_matches_trove_ui_s_link()
    {
        // Trove.UI's Settings links to "desktop/change-cora-address" under the app's /trove/ base.
        Assert.Equal("/trove/desktop/change-cora-address", ShellRoutes.ChangeAddress);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (X11) AppleWebKit/605", "Mozilla/5.0 (X11) AppleWebKit/605 TroveDesktop/0.1.0")]
    [InlineData("Mozilla/5.0 TroveDesktop/0.1.0", "Mozilla/5.0 TroveDesktop/0.1.0")]
    [InlineData(null, "TroveDesktop/0.1.0")]
    public void The_user_agent_gets_the_token_once(string? agent, string expected) =>
        Assert.Equal(expected, ShellRoutes.WithToken(agent, "0.1.0"));

    [Fact]
    public async Task Settings_survive_a_restart_and_keep_keys_they_dont_know()
    {
        using var temp = new TempHome();
        File.WriteAllText(temp.Home.SettingsFile, """{ "window": { "width": 900 } }""");

        await temp.Settings.SaveAsync(new DesktopPreferences("https://cora.example.ts.net", "wopr", OpenedTrove: true));

        Assert.Equal(new DesktopPreferences("https://cora.example.ts.net", "wopr", true), temp.Settings.Load());
        Assert.Contains("\"width\": 900", File.ReadAllText(temp.Home.SettingsFile));
    }

    [Fact]
    public void An_unreadable_settings_file_reads_as_a_first_start()
    {
        using var temp = new TempHome();
        File.WriteAllText(temp.Home.SettingsFile, "{ not json");

        Assert.Equal(new DesktopPreferences(), temp.Settings.Load());
    }

    [Fact]
    public void Trove_home_moves_with_trove_home_variable()
    {
        var before = Environment.GetEnvironmentVariable(TroveHome.Variable);
        try
        {
            var folder = Path.Combine(Path.GetTempPath(), "trove-elsewhere");
            Environment.SetEnvironmentVariable(TroveHome.Variable, folder);
            Assert.Equal(Path.Combine(folder, "desktop.json"), TroveHome.ForUser().SettingsFile);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TroveHome.Variable, before);
        }
    }
}
