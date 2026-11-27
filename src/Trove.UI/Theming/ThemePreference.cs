namespace Trove.UI.Theming;

/// <summary>
/// What the person chose in the theme picker, the way every HopeLab app keeps it: a theme id, or <see cref="System"/>,
/// which follows the device between Light and Dark and is the default. Mirrors CORA's <c>themePreference.ts</c>.
/// </summary>
public static class ThemePreference
{
    /// <summary>The absence of a choice: Light or Dark, whichever the device asks for.</summary>
    public const string System = "system";

    /// <summary>Every choice in the picker's order: System, the light themes, then the dark ones.</summary>
    public static IReadOnlyList<string> All { get; } =
        [System, .. ThemeCatalog.All.Where(t => t.IsLight).Select(t => t.Id), .. ThemeCatalog.All.Where(t => !t.IsLight).Select(t => t.Id)];

    /// <summary>A stored value read as a preference. Nothing, or anything unrecognised, is <see cref="System"/>.</summary>
    public static string Read(string? stored) =>
        stored is not null && ThemeCatalog.All.Any(t => t.Id == stored) ? stored : System;

    /// <summary>The theme drawn for a preference.</summary>
    public static ThemeInfo Resolve(string preference, bool systemDark) =>
        preference == System ? ThemeCatalog.Find(systemDark ? "dark" : "light") : ThemeCatalog.Find(preference);

    /// <summary>The name the header button gives a preference, such as "System (Dark)".</summary>
    public static string Label(string preference, bool systemDark) =>
        preference == System ? $"System ({Resolve(preference, systemDark).Name})" : ThemeCatalog.Find(preference).Name;

    /// <summary>
    /// The preference an arrow, Home or End key moves to, wrapping at the ends as a radio group does. Null for any
    /// other key, which the picker leaves alone.
    /// </summary>
    public static string? ForKey(string preference, string key)
    {
        var count = All.Count;
        var i = Math.Max(0, IndexOf(preference));
        return key switch
        {
            "ArrowRight" or "ArrowDown" => All[(i + 1) % count],
            "ArrowLeft" or "ArrowUp" => All[(i - 1 + count) % count],
            "Home" => All[0],
            "End" => All[count - 1],
            _ => null,
        };
    }

    private static int IndexOf(string preference)
    {
        for (var i = 0; i < All.Count; i++)
        {
            if (All[i] == preference) return i;
        }
        return -1;
    }
}
