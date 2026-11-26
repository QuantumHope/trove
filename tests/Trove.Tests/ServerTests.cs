using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trove.Tests;

public sealed class ServerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/")]
    [InlineData("/library")]
    [InlineData("/goals")]
    [InlineData("/sources")]
    [InlineData("/settings")]
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
}
