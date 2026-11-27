namespace Trove.UI;

/// <summary>Pages in CORA, which serves Trove from the same address (Trove lives at /trove/, CORA at /).</summary>
public static class CoraLinks
{
    /// <summary>Where an admin enables the Trove module and sets CORA's address, the YouTube key and the channels.</summary>
    public const string Modules = "/admin/modules";

    /// <summary>Where feeds are followed, and where each one is set to send its posts to Trove.</summary>
    public const string Feeds = "/admin/feeds";

    /// <summary>A changeset in CORA: the text CORA read from a page and what it proposes from it.</summary>
    public static string Changeset(string id) => "/admin/changesets/" + Uri.EscapeDataString(id);

    /// <summary>The text CORA pulled for a changeset, where you check it and press Analyze.</summary>
    public static string ChangesetText(string id) => Changeset(id) + "/document";

    /// <summary>A document in CORA's library, to read what CORA holds from a page.</summary>
    public static string LibraryDocument(string id) => "/knowledge/library/" + Uri.EscapeDataString(id);

    /// <summary>Where the CORA row's action goes when it opens CORA; null when it asks CORA to read the page instead.</summary>
    public static string? For(Trove.Core.Articles.CoraKnowledge cora) => (cora.Action, cora.ChangesetId) switch
    {
        (Trove.Core.Articles.CoraAction.CheckText, { } id) => ChangesetText(id),
        (Trove.Core.Articles.CoraAction.Review or Trove.Core.Articles.CoraAction.ViewProgress, { } id) => Changeset(id),
        (Trove.Core.Articles.CoraAction.View, { } id) => LibraryDocument(id),
        _ => null,
    };
}
