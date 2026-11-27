using System.Globalization;

namespace Trove.UI.Playthroughs;

/// <summary>Lengths, views and dates the way YouTube shows them on a video card.</summary>
public static class VideoFormat
{
    public static string Length(TimeSpan length) => length.TotalHours >= 1
        ? $"{(int)length.TotalHours}:{length.Minutes:00}:{length.Seconds:00}"
        : $"{length.Minutes}:{length.Seconds:00}";

    public static string Views(long? views) => views switch
    {
        null => "",
        1 => "1 view",
        < 1_000 => $"{views} views",
        < 1_000_000 => $"{Short(views.Value / 1_000d)}K views",
        < 1_000_000_000 => $"{Short(views.Value / 1_000_000d)}M views",
        _ => $"{Short(views.Value / 1_000_000_000d)}B views",
    };

    public static string Ago(DateTimeOffset when, DateTimeOffset now)
    {
        var days = (now - when).TotalDays;
        return days switch
        {
            < 1 => "today",
            < 2 => "1 day ago",
            < 7 => $"{(int)days} days ago",
            < 14 => "1 week ago",
            < 30 => $"{(int)(days / 7)} weeks ago",
            < 60 => "1 month ago",
            < 365 => $"{(int)(days / 30)} months ago",
            < 730 => "1 year ago",
            _ => $"{(int)(days / 365)} years ago",
        };
    }

    public static string Day(DateOnly day) => day.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);

    public static string Stars(decimal rating) => rating.ToString("0.##", CultureInfo.InvariantCulture);

    // YouTube shows one decimal under 10 (1.2M) and none above (12M).
    private static string Short(double value) =>
        (value < 10 ? Math.Floor(value * 10) / 10 : Math.Floor(value)).ToString("0.#", CultureInfo.InvariantCulture);
}
