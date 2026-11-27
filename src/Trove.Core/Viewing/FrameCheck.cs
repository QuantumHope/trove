using System.Text.Json.Serialization;

namespace Trove.Core.Viewing;

/// <summary>Whether a page will show inside Trove's reader. CORA reads the page's headers to say (ADR-132 in CORA).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Framing>))]
public enum Framing
{
    /// <summary>
    /// CORA couldn't tell, so the reader tries the frame and offers a new tab beside it. First, so an answer that
    /// leaves the value out reads as Unknown rather than Allowed.
    /// </summary>
    Unknown,

    /// <summary>Nothing the page sent forbids it.</summary>
    Allowed,

    /// <summary>The site forbids it, or a browser would refuse it (plain http).</summary>
    Blocked,
}

/// <summary>CORA's answer from <c>GET /web/frame-check</c>.</summary>
/// <param name="Reason">Why, in a sentence, when it isn't plainly allowed.</param>
public sealed record FrameCheck(string Url, Framing Framing, string? Reason = null);

/// <summary>Asks CORA whether a page can be framed.</summary>
public interface IFrameChecker
{
    /// <summary>Never throws for a page CORA couldn't read: that's <see cref="Framing.Unknown"/>.</summary>
    Task<FrameCheck> CheckAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>What the reader decides before it asks CORA.</summary>
public static class ReaderRules
{
    /// <summary>
    /// Why Trove won't frame this address itself, or null when it may ask CORA. A page on Trove's own address would
    /// share CORA's sign-in and could reach out of the sandbox, so it always opens in a new tab; so does anything that
    /// isn't a web page over https, which a browser blocks inside an https app anyway.
    /// </summary>
    public static string? WontFrame(string url, Uri appAddress)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var address) || (address.Scheme != Uri.UriSchemeHttps && address.Scheme != Uri.UriSchemeHttp))
        {
            return "This link isn't a web page Trove can show.";
        }

        // A name or password in the link would hide which site it is, and browsers won't send them to a frame anyway.
        if (address.UserInfo.Length > 0)
        {
            return "This link carries a sign-in, so it opens in its own tab.";
        }

        // Scheme, host and port only: the same site whatever else the link carries.
        if (Uri.Compare(address, appAddress, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
        {
            return "This page is part of CORA, so it opens in its own tab.";
        }

        if (address.Scheme == Uri.UriSchemeHttp && appAddress.Scheme == Uri.UriSchemeHttps)
        {
            return $"{Site(address)} isn't served over https, so a browser won't show it inside Trove.";
        }

        return null;
    }

    /// <summary>Opening an article to read it moves it to Reading from New or Read next.</summary>
    public static Articles.ReadState? OnOpen(Articles.ReadState state) =>
        state is Articles.ReadState.New or Articles.ReadState.ReadNext ? Articles.ReadState.Reading : null;

    /// <summary>Pressing play moves a video to In progress from New or Watch next.</summary>
    public static Playthroughs.WatchState? OnPlay(Playthroughs.WatchState state) =>
        state is Playthroughs.WatchState.New or Playthroughs.WatchState.WatchNext ? Playthroughs.WatchState.InProgress : null;

    private static string Site(Uri address) =>
        address.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? address.Host[4..] : address.Host;
}
