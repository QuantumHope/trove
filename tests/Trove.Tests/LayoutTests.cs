using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Setup;
using Trove.UI.Setup;
using Trove.UI.Layout;
using Trove.UI.Theming;

namespace Trove.Tests;

public sealed class LayoutTests : BunitContext
{
    public LayoutTests()
    {
        JSInterop.Setup<string?>("trove.theme.get").SetResult("wopr");
        JSInterop.Setup<bool>("trove.theme.systemDark").SetResult(true);
        JSInterop.SetupVoid("trove.theme.set", _ => true).SetVoidResult();
        JSInterop.SetupVoid("trove.theme.bindList", _ => true).SetVoidResult();
        JSInterop.SetupVoid("trove.theme.unbindList", _ => true).SetVoidResult();
        JSInterop.SetupVoid("trove.theme.focusChecked", _ => true).SetVoidResult();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ITroveSetup>(new FakeSetup(new SetupStatus(true, true, 1)));
    }

    [Fact]
    public void Header_and_bottom_bar_link_every_main_area()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, "body"))));

        foreach (var nav in new[] { "nav.trove-topnav", "nav.trove-bottomnav" })
        {
            var links = cut.FindAll($"{nav} a");
            Assert.Equal(NavArea.All.Select(a => a.Title), links.Select(l => l.QuerySelector(".trove-nav-label")!.TextContent));
        }
    }

    [Fact]
    public void Theme_button_names_the_saved_theme_and_opens_the_list()
    {
        var cut = Render<ThemeToggle>();

        cut.WaitForAssertion(() => Assert.Equal("Theme: WOPR. Choose a theme", cut.Find("button").GetAttribute("aria-label")));
        Assert.Empty(cut.FindAll(".theme-list"));

        cut.Find("button.theme-toggle-btn").Click();

        Assert.NotNull(cut.Find(".theme-list[role=radiogroup]"));
    }

    [Fact]
    public void Theme_list_offers_system_then_light_then_dark_and_checks_the_saved_one()
    {
        var cut = Render<ThemeList>();

        var options = cut.FindAll("[role=radio]");
        Assert.Equal(ThemePreference.All, options.Select(o => o.GetAttribute("data-preference")));
        Assert.Equal(["LIGHT THEMES", "DARK THEMES"], cut.FindAll(".theme-list__heading").Select(h => h.TextContent));
        cut.WaitForAssertion(() => Assert.Equal("wopr", cut.Find("[aria-checked=true]").GetAttribute("data-preference")));
        Assert.Equal("0", cut.Find("[aria-checked=true]").GetAttribute("tabindex"));
    }

    [Fact]
    public void Picking_a_theme_saves_it()
    {
        var cut = Render<ThemeList>();

        cut.Find("[data-preference=lcars]").Click();

        var call = JSInterop.VerifyInvoke("trove.theme.set");
        Assert.Equal("lcars", call.Arguments[0]);
    }

    [Fact]
    public void Arrow_keys_move_the_choice_and_wrap()
    {
        var first = ThemePreference.All[0];
        var last = ThemePreference.All[^1];

        Assert.Equal(ThemePreference.All[1], ThemePreference.ForKey(first, "ArrowDown"));
        Assert.Equal(last, ThemePreference.ForKey(first, "ArrowUp"));
        Assert.Equal(first, ThemePreference.ForKey(last, "ArrowRight"));
        Assert.Equal(first, ThemePreference.ForKey("wopr", "Home"));
        Assert.Equal(last, ThemePreference.ForKey("wopr", "End"));
        Assert.Null(ThemePreference.ForKey("wopr", "Tab"));
    }

    [Fact]
    public void Unknown_or_missing_saved_values_read_as_system()
    {
        Assert.Equal(ThemePreference.System, ThemePreference.Read(null));
        Assert.Equal(ThemePreference.System, ThemePreference.Read("retired-theme"));
        Assert.Equal("System (Light)", ThemePreference.Label(ThemePreference.System, systemDark: false));
        Assert.Equal("Dark", ThemePreference.Resolve(ThemePreference.System, systemDark: true).Name);
    }
}
