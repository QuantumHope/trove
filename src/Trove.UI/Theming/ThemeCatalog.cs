namespace Trove.UI.Theming;

/// <summary>One shared HopeLab theme as the picker shows it.</summary>
/// <param name="Id">The theme's id, the same in every HopeLab app and in the CSS (<c>data-theme</c>).</param>
/// <param name="Name">The name the picker shows.</param>
/// <param name="Description">One line about the theme.</param>
/// <param name="IsLight">Whether the theme is light (its CSS sets <c>color-scheme: light</c>).</param>
public sealed record ThemeInfo(string Id, string Name, string Description, bool IsLight);

/// <summary>
/// The shared themes, in the same order as <c>MattEland.HopeLab.Themes.ThemeCatalog</c> (light first, then dark). That
/// catalog lives in the package's Avalonia library, which a web app can't load, so this list mirrors it and
/// <c>ThemeCatalogTests</c> checks it against the CSS the package ships: a theme added there fails the build here.
/// </summary>
public static class ThemeCatalog
{
    /// <summary>The theme a new install starts on: the package's own default, which its CSS also puts on <c>:root</c>.</summary>
    public const string DefaultId = "dark";

    public static IReadOnlyList<ThemeInfo> All { get; } =
    [
        new("light", "Light", "CORA on paper", IsLight: true),
        new("hc-light", "High contrast light", "Black on white, 7:1 text, solid borders", IsLight: true),
        new("glados", "GLaDOS", "Enrichment Center test chamber", IsLight: true),
        new("clippy", "Clippy", "Office 97 assistant", IsLight: true),
        new("samantha", "Samantha", "OS1 from Her, warm and close", IsLight: true),
        new("gerty", "GERTY", "Sarang lunar base", IsLight: true),
        new("dark", "Dark", "CORA in the void", IsLight: false),
        new("hc-dark", "High contrast dark", "White on black, 7:1 text, solid borders", IsLight: false),
        new("shodan", "SHODAN", "Citadel Station, deck by deck", IsLight: false),
        new("muther", "MUTHER", "MU-TH-UR 6000 phosphor terminal", IsLight: false),
        new("hal", "HAL 9000", "Discovery One, the red eye", IsLight: false),
        new("mechanicus", "Adeptus Mechanicus", "Forge-world cogitator", IsLight: false),
        new("cortana", "Cortana", "UNSC smart AI, hologram", IsLight: false),
        new("helios", "Helios", "Area 51, Aquinas hub", IsLight: false),
        new("daedalus", "Daedalus", "ECHELON signals intercept", IsLight: false),
        new("icarus", "Icarus", "Majestic 12 countermeasure AI", IsLight: false),
        new("eliza", "Eliza Cassan", "Picus newsroom, black and gold", IsLight: false),
        new("lcars", "LCARS", "Library computer, 24th-century panels", IsLight: false),
        new("mcp", "MCP", "Master Control Program, the Grid", IsLight: false),
        new("skynet", "Skynet", "Cyberdyne defence network, T-800 HUD", IsLight: false),
        new("jarvis", "JARVIS", "Stark workshop and helmet HUD", IsLight: false),
        new("wopr", "WOPR", "War Operation Plan Response", IsLight: false),
        new("kitt", "KITT", "Knight Industries Two Thousand", IsLight: false),
        new("cylon", "Cylon", "Basestar Hybrid, Battlestar Galactica (2004)", IsLight: false),
    ];

    /// <summary>The theme with this id, or the default when the id is unknown (an old or hand-edited saved choice).</summary>
    public static ThemeInfo Find(string? id) =>
        All.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.Ordinal)) ?? All.First(t => t.Id == DefaultId);
}
