using Microsoft.AspNetCore.Components;
using Trove.UI.Layout;
using Trove.UI.Theming;

namespace Trove.Tests;

public sealed class LayoutTests : BunitContext
{
    public LayoutTests()
    {
        JSInterop.Setup<string?>("trove.theme.get").SetResult("wopr");
        JSInterop.SetupVoid("trove.theme.set", _ => true);
    }

    [Fact]
    public void Rail_links_every_main_area()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, "body"))));

        var links = cut.FindAll("nav.trove-nav a");
        Assert.Equal(NavArea.All.Select(a => a.Title), links.Select(l => l.QuerySelector(".trove-nav-label")!.TextContent));
    }

    [Fact]
    public void Theme_picker_offers_every_shared_theme_and_shows_the_saved_one()
    {
        var cut = Render<ThemePicker>();

        var select = cut.Find("select");
        Assert.Equal(ThemeCatalog.All.Count, select.QuerySelectorAll("option").Length);
        cut.WaitForAssertion(() => Assert.Equal("wopr", cut.Find("option[selected]").GetAttribute("value")));
    }

    [Fact]
    public void Picking_a_theme_saves_it()
    {
        var cut = Render<ThemePicker>();

        cut.Find("select").Change("lcars");

        var call = JSInterop.VerifyInvoke("trove.theme.set");
        Assert.Equal("lcars", call.Arguments[0]);
    }
}
