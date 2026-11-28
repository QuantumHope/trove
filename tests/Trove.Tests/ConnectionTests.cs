using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Setup;
using Trove.UI.Setup;
using Trove.UI.Connection;
using Trove.UI.Desktop;
using Trove.UI.Pages;

namespace Trove.Tests;

public sealed class ConnectionTests : BunitContext
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.CreateCustomTimeZone("test-eastern", TimeSpan.FromHours(-5), "Eastern", "Eastern");
    private static readonly DateTimeOffset Now = new(2026, 11, 28, 20, 0, 0, TimeSpan.Zero); // 3:00 PM Eastern

    [Theory]
    [InlineData("2026-11-28T19:14:00Z", "at 2:14 PM")]
    [InlineData("2026-11-27T19:14:00Z", "yesterday at 2:14 PM")]
    [InlineData("2026-11-26T19:14:00Z", "on Nov 26 at 2:14 PM")]
    [InlineData("2025-12-30T19:14:00Z", "on Dec 30, 2025 at 2:14 PM")]
    // 2 AM UTC on the 28th is still the evening of the 27th in Eastern time.
    [InlineData("2026-11-28T02:00:00Z", "yesterday at 9:00 PM")]
    public void Describe_names_the_day_in_the_viewers_own_time_zone(string saved, string expected) =>
        Assert.Equal(expected, ConnectionStatus.Describe(DateTimeOffset.Parse(saved), Now, Eastern));

    [Fact]
    public void A_saved_copy_marks_offline_and_a_live_answer_marks_online_again()
    {
        var status = new ConnectionStatus();
        var changes = 0;
        status.Changed += () => changes++;

        status.Observe(Saved("2026-11-28T19:14:00Z"));
        Assert.True(status.IsOffline);
        Assert.Equal(DateTimeOffset.Parse("2026-11-28T19:14:00Z"), status.SavedAt);

        status.Observe(new HttpResponseMessage(HttpStatusCode.OK));
        Assert.False(status.IsOffline);
        Assert.Null(status.SavedAt);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Several_saved_copies_report_the_oldest()
    {
        var status = new ConnectionStatus();

        status.Observe(Saved("2026-11-28T19:14:00Z"));
        status.Observe(Saved("2026-11-27T10:00:00Z"));
        status.Observe(Saved("2026-11-28T19:30:00Z"));

        Assert.Equal(DateTimeOffset.Parse("2026-11-27T10:00:00Z"), status.SavedAt);
    }

    [Fact]
    public async Task A_read_that_reaches_nothing_says_there_is_no_saved_copy()
    {
        var status = new ConnectionStatus();
        using var client = Client(status, new Unreachable());

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("api/playthroughs"));

        Assert.Equal(ConnectionHandler.NothingSaved, ex.Message);
        Assert.True(status.IsOffline);
    }

    [Fact]
    public async Task A_change_that_reaches_nothing_says_to_try_again_once_online()
    {
        var status = new ConnectionStatus();
        using var client = Client(status, new Unreachable());

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.PostAsync("api/playthroughs", null));

        Assert.Equal(ConnectionHandler.ChangeNotSaved, ex.Message);
    }

    [Fact]
    public async Task An_error_from_cora_itself_passes_through_untouched()
    {
        var status = new ConnectionStatus();
        using var client = Client(status, new Answers(new HttpResponseMessage(HttpStatusCode.BadGateway)));

        var response = await client.GetAsync("api/playthroughs");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.False(status.IsOffline);
    }

    [Fact]
    public void Banner_says_how_old_the_board_is_and_goes_away_when_cora_answers()
    {
        var status = UseStatus();
        var banner = Render<OfflineBanner>();
        Assert.Empty(banner.FindAll(".trove-offline"));

        status.Observe(Saved("2026-11-28T19:14:00Z"));

        banner.WaitForAssertion(() => Assert.Equal(
            "CORA can't be reached, so this is what Trove saved at 2:14 PM. Changes won't save until it's back.",
            banner.Find(".trove-offline-text").TextContent));

        status.Observe(new HttpResponseMessage(HttpStatusCode.OK));

        banner.WaitForAssertion(() => Assert.Empty(banner.FindAll(".trove-offline")));
    }

    [Fact]
    public void Banner_without_a_saved_copy_just_says_cora_cant_be_reached()
    {
        var status = UseStatus();
        var banner = Render<OfflineBanner>();

        status.Unreachable();

        banner.WaitForAssertion(() => Assert.Equal(
            "CORA can't be reached right now. Changes won't save until it's back.",
            banner.Find(".trove-offline-text").TextContent));
    }

    [Fact]
    public void Settings_in_the_desktop_window_shows_its_address_and_the_change_button()
    {
        UseSettings();
        JSInterop.SetupModule("./_content/Trove.UI/desktop.js")
            .Setup<DesktopInfo?>("shell").SetResult(new DesktopInfo("0.1.0", "https://cora.example.ts.net"));

        var page = Render<Settings>();

        page.WaitForAssertion(() => Assert.Contains("https://cora.example.ts.net", page.Find("#window-heading").ParentElement!.TextContent));
        Assert.Equal(DesktopShell.ChangeAddressPath, page.Find("a[href='desktop/change-cora-address']").GetAttribute("href"));
    }

    [Fact]
    public void Settings_in_a_browser_has_no_window_card()
    {
        UseSettings();
        JSInterop.SetupModule("./_content/Trove.UI/desktop.js").Setup<DesktopInfo?>("shell").SetResult(null);

        var page = Render<Settings>();

        Assert.Empty(page.FindAll("#window-heading"));
    }

    private ConnectionStatus UseStatus()
    {
        var status = new ConnectionStatus();
        Services.AddSingleton(status);
        Services.AddSingleton<TimeProvider>(new FixedClock(Now, Eastern));
        return status;
    }

    private void UseSettings()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ITroveSetup>(new FakeSetup(new SetupStatus(true, true, 1)));
        Services.AddScoped<DesktopShell>();
    }

    private static HttpResponseMessage Saved(string at)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add(ConnectionStatus.SavedAtHeader, at);
        return response;
    }

    private static HttpClient Client(ConnectionStatus status, HttpMessageHandler inner) =>
        new(new ConnectionHandler(status) { InnerHandler = inner }) { BaseAddress = new Uri("http://localhost/trove/") };

    private sealed class Unreachable : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("TypeError: Failed to fetch");
    }

    private sealed class Answers(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }

    private sealed class FixedClock(DateTimeOffset now, TimeZoneInfo zone) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => zone;
    }
}
