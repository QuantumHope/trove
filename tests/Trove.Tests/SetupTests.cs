using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trove.Core.Setup;
using Trove.UI.Layout;
using Trove.UI.Pages;
using Trove.UI.Setup;

namespace Trove.Tests;

public sealed class SetupTests : BunitContext
{
    public SetupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("trove.theme.get").SetResult("light");
        JSInterop.Setup<bool>("trove.theme.systemDark").SetResult(false);
    }

    [Fact]
    public void Starting_without_coras_address_asks_for_it_with_this_sites_address_filled_in()
    {
        var setup = Use(new SetupStatus(false, true, 1));

        var layout = RenderLayout();

        var input = layout.Find("#cora-address");
        Assert.Equal("http://localhost", input.GetAttribute("value"));
        layout.Find("form.trove-setup-form").Submit();

        Assert.Equal(["http://localhost"], setup.Saved);
        Assert.Empty(layout.FindAll("[role=dialog]"));
        Assert.Contains("Trove can reach CORA", layout.Find(".trove-toast").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Starting_with_coras_address_set_asks_nothing()
    {
        Use(new SetupStatus(true, false, 0));

        Assert.Empty(RenderLayout().FindAll("[role=dialog]"));
    }

    [Fact]
    public void An_address_the_module_turns_down_says_why_and_stays_open()
    {
        var setup = Use(new SetupStatus(false, true, 1));
        setup.Refusal = "Trove couldn't reach https://nope from where the module runs.";
        var layout = RenderLayout();

        layout.Find("#cora-address").Input("https://nope");
        layout.Find("form.trove-setup-form").Submit();

        Assert.Equal(setup.Refusal, layout.Find("[role=alert]").TextContent);
        Assert.NotNull(layout.Find("[role=dialog]"));
    }

    [Fact]
    public void Someone_who_is_not_coras_admin_is_told_who_can_set_it()
    {
        var setup = Use(new SetupStatus(false, true, 1));
        setup.NotAdmin = true;
        var layout = RenderLayout();

        layout.Find("form.trove-setup-form").Submit();

        Assert.Contains("Only CORA's admin", layout.Find("[role=alert]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Later_closes_the_prompt()
    {
        Use(new SetupStatus(false, true, 1));
        var layout = RenderLayout();

        layout.FindAll("button").Single(b => b.TextContent == "Later").Click();

        Assert.Empty(layout.FindAll("[role=dialog]"));
    }

    [Fact]
    public void Settings_lists_what_is_set_and_what_is_missing()
    {
        Use(new SetupStatus(true, false, 0));

        var page = Render<Settings>();

        var items = page.FindAll(".trove-setup-list li");
        Assert.Equal(["is-set", "is-missing", "is-missing"], items.Select(i => i.ClassName));
        Assert.Contains("No YouTube key yet", items[1].TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain(page.FindAll("button"), b => b.TextContent == "Set CORA's address");
    }

    private FakeSetup Use(SetupStatus status)
    {
        var setup = new FakeSetup(status);
        Services.AddSingleton<ITroveSetup>(setup);
        return setup;
    }

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(p => p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, "body"))));
}

internal sealed class FakeSetup(SetupStatus status) : ITroveSetup
{
    public List<string> Saved { get; } = [];

    public string? Refusal { get; set; }

    public bool NotAdmin { get; set; }

    public Task<SetupStatus> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(status);

    public Task<SaveCoraAddressResult> SaveCoraAddressAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        if (NotAdmin)
        {
            throw new UnauthorizedAccessException(HttpTroveSetup.NotAdmin);
        }

        if (Refusal is not null)
        {
            return Task.FromResult(new SaveCoraAddressResult(false, Refusal, null));
        }

        Saved.Add(baseUrl);
        status = status with { CoraAddressSet = true };
        return Task.FromResult(new SaveCoraAddressResult(true, null, new CoraAddress(baseUrl, CoraAddressSource.Saved, null)));
    }
}
