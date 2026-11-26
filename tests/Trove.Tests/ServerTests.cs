using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trove.Tests;

public sealed class ServerTests(TroveFactory factory) : IClassFixture<TroveFactory>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/library")]
    [InlineData("/goals")]
    [InlineData("/sources")]
    [InlineData("/settings")]
    [InlineData("/library/youtube")]
    public async Task Main_areas_render(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.EnsureSuccessStatusCode();
        Assert.Contains("trove-nav", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/_content/Trove.UI/themes/index.css")]
    [InlineData("/_content/Trove.UI/themes/wopr.css")]
    [InlineData("/_content/Trove.UI/trove.css")]
    [InlineData("/manifest.webmanifest")]
    [InlineData("/service-worker.js")]
    public async Task Shell_assets_are_served(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_addresses_get_the_not_found_page()
    {
        var response = await factory.CreateClient().GetAsync("/no-such-page");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Not found", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_production_error_page_renders_on_the_server()
    {
        var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var response = await production.CreateClient().GetAsync("/Error");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Something went wrong", html, StringComparison.Ordinal);
        Assert.Contains("trove-nav", html, StringComparison.Ordinal);
    }
}
