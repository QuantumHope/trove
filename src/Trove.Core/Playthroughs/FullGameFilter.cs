using System.Text.RegularExpressions;

namespace Trove.Core.Playthroughs;

/// <summary>
/// Picks the single-video full playthroughs out of a channel's uploads. MKIceAndFire also puts "FULL GAME" on the parts
/// of a multi-part series ("STARFIELD Gameplay Walkthrough Part 1 FULL GAME [4K 60FPS] - No Commentary"), so a title
/// has to say FULL GAME and not number a part of the walkthrough. A part in the game's own name ("THE LAST OF US PART I")
/// comes before the word Walkthrough or Gameplay, so it doesn't count.
/// </summary>
public static partial class FullGameFilter
{
    public static bool IsFullGame(string title) =>
        FullGame().IsMatch(title) && !NumberedPart().IsMatch(title);

    [GeneratedRegex(@"\bfull\s+game\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FullGame();

    // "Walkthrough Part 1", "Gameplay Part 12", "Playthrough PART II", "Walkthrough Part One": one piece of a series.
    [GeneratedRegex(@"\b(walkthrough|gameplay|playthrough|let'?s\s+play)\s+part\s*(\d+|[ivx]+|one|two|three|four|five|six|seven|eight|nine|ten)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NumberedPart();
}
