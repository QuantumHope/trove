namespace Trove.UI;

/// <summary>Pages in CORA, which serves Trove from the same address (Trove lives at /trove/, CORA at /).</summary>
public static class CoraLinks
{
    /// <summary>Where an admin enables the Trove module and sets its YouTube key.</summary>
    public const string Modules = "/admin/modules";

    /// <summary>Where feeds are followed, and where each one is set to send its posts to Trove.</summary>
    public const string Feeds = "/admin/feeds";

    /// <summary>A changeset in CORA: the text CORA read from a page and what it proposes from it.</summary>
    public static string Changeset(string id) => "/admin/changesets/" + Uri.EscapeDataString(id);
}
