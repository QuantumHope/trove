namespace Trove.UI.Layout;

/// <summary>Placeholder names for the Library screen until the item model lands.</summary>
public static class MediaKinds
{
    public static IReadOnlyList<string> All { get; } = ["Books", "Video", "Movies and TV", "Games", "Board games", "Articles"];

    public static IReadOnlyList<string> Statuses { get; } = ["Wishlist", "Up next", "In progress", "Done", "Dropped"];
}
