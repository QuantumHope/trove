using Trove.UI.Pages;

namespace Trove.Tests;

public sealed class SourcesPageTests : BunitContext
{
    [Fact]
    public void Each_set_up_source_links_by_its_own_name()
    {
        var page = Render<Sources>();

        var links = page.FindAll("a.trove-pill").ToDictionary(a => a.GetAttribute("href")!, a => a.TextContent.Trim());

        Assert.Equal("Full playthroughs", links["library/youtube"]);
        Assert.Equal("Reading list", links["library/articles"]);
    }
}
