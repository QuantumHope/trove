namespace Trove.UI.Layout;

/// <summary>One of Trove's main areas, as the header's tab strip (and the bottom bar on a phone) lists it.</summary>
/// <param name="Title">The area's name.</param>
/// <param name="Href">Its address, relative to the app's base.</param>
/// <param name="Glyph">A one-character mark for the tab.</param>
/// <param name="Summary">One line on what the area is for.</param>
public sealed record NavArea(string Title, string Href, string Glyph, string Summary)
{
    public static IReadOnlyList<NavArea> All { get; } =
    [
        new("Home", "", "◆", "What you're in the middle of and what's up next"),
        new("Library", "library", "▤", "Everything in Trove, by status and by kind"),
        new("Goals", "goals", "◎", "This year's goals and how they're pacing"),
        new("Sources", "sources", "⇣", "Imports and the places Trove reads from"),
        new("Settings", "settings", "⚙", "Theme and app settings"),
    ];
}
