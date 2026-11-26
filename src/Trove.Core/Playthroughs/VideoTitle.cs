using System.Text.RegularExpressions;

namespace Trove.Core.Playthroughs;

/// <summary>
/// Splits an upload's title into the game and the capture details, so the board can lead with the game:
/// <c>STRAY Gameplay Walkthrough FULL GAME [4K 60FPS PS5] - No Commentary</c> becomes <c>STRAY</c> and
/// <c>4K 60FPS PS5</c>. A title that doesn't follow that pattern is kept whole.
/// </summary>
public static partial class VideoTitle
{
    public static (string Game, string? Details) Split(string title)
    {
        var details = Bracketed().Matches(title).Select(m => m.Groups[1].Value.Trim()).FirstOrDefault(d => d.Length > 0);
        var marker = Marker().Match(title);
        if (!marker.Success || marker.Index == 0)
        {
            return (title.Trim(), details);
        }

        var game = title[..marker.Index].Trim().TrimEnd('-', '–', '—', ':', '|', '(').Trim();
        return game.Length == 0 ? (title.Trim(), details) : (game, details);
    }

    [GeneratedRegex(@"\b(gameplay|walkthrough|playthrough|full\s+game|let'?s\s+play)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Marker();

    [GeneratedRegex(@"\[([^\]]*)\]")]
    private static partial Regex Bracketed();
}
