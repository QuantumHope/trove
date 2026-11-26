using System.Reflection;
using System.Text.RegularExpressions;
using Trove.UI.Theming;

namespace Trove.Tests;

/// <summary>
/// Trove's theme list mirrors the shared package's catalog, so these check it against the CSS the restored
/// MattEland.HopeLab.Themes ships: a theme added, renamed or removed there fails here until the list catches up.
/// </summary>
public sealed partial class ThemeCatalogTests
{
    private static readonly string CssFolder = typeof(ThemeCatalogTests).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(a => a.Key == "HopeLabThemesCss").Value!;

    private static IReadOnlyList<string> PackageThemeIds() =>
        ImportRegex().Matches(File.ReadAllText(Path.Combine(CssFolder, "index.css")))
            .Select(m => m.Groups["id"].Value)
            .Where(id => id != "base")
            .ToList();

    [Fact]
    public void Lists_every_theme_the_package_ships_and_no_others()
    {
        Assert.Equal(PackageThemeIds().Order(), ThemeCatalog.All.Select(t => t.Id).Order());
    }

    [Fact]
    public void Names_descriptions_and_lightness_match_each_themes_css()
    {
        foreach (var theme in ThemeCatalog.All)
        {
            var css = File.ReadAllText(Path.Combine(CssFolder, theme.Id + ".css"));
            Assert.Contains($"{theme.Name}: {theme.Description}", css, StringComparison.Ordinal);
            Assert.Contains($"color-scheme: {(theme.IsLight ? "light" : "dark")};", css, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Default_is_the_packages_default()
    {
        // index.css imports the default theme first, because its block is also on :root.
        Assert.Equal(ThemeCatalog.DefaultId, PackageThemeIds()[0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-theme")]
    public void Unknown_saved_ids_fall_back_to_the_default(string? id)
    {
        Assert.Equal(ThemeCatalog.DefaultId, ThemeCatalog.Find(id).Id);
    }

    [Fact]
    public void Ids_are_unique()
    {
        Assert.Equal(ThemeCatalog.All.Count, ThemeCatalog.All.Select(t => t.Id).Distinct().Count());
    }

    [GeneratedRegex("""@import\s+"\./(?<id>[a-z0-9-]+)\.css";""")]
    private static partial Regex ImportRegex();
}
