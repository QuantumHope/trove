namespace Trove.UI;

/// <summary>Pages in CORA, which serves Trove from the same address (Trove lives at /trove/, CORA at /).</summary>
public static class CoraLinks
{
    /// <summary>Where an admin enables the Trove module and sets its YouTube key.</summary>
    public const string Modules = "/admin/modules";

    /// <summary>Where feeds are followed, and where each one is set to send its posts to Trove.</summary>
    public const string Feeds = "/admin/feeds";
}
