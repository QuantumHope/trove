using System.Globalization;

namespace Trove.UI.Connection;

/// <summary>
/// Whether Trove is showing what CORA just sent or a copy the service worker saved earlier. When CORA can't be reached,
/// the service worker answers an API read from its last copy and marks it with <see cref="SavedAtHeader"/>, so the app
/// can say it's offline and how old the board is. Any answer without the mark means CORA answered again.
/// </summary>
public sealed class ConnectionStatus
{
    /// <summary>The header the service worker puts on a saved copy: when it was saved, in ISO 8601 (UTC).</summary>
    public const string SavedAtHeader = "X-Trove-Saved-At";

    /// <summary>When the copy on screen was saved; null while CORA is answering.</summary>
    public DateTimeOffset? SavedAt { get; private set; }

    /// <summary>True when the last answer was a saved copy, or the last call couldn't reach CORA at all.</summary>
    public bool IsOffline { get; private set; }

    /// <summary>Raised when <see cref="IsOffline"/> or <see cref="SavedAt"/> changes.</summary>
    public event Action? Changed;

    /// <summary>Reads one answer: a saved copy marks Trove offline as of its time; anything else marks it online.</summary>
    public void Observe(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(SavedAtHeader, out var values)
            && DateTimeOffset.TryParse(values.FirstOrDefault(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var saved))
        {
            // Several saved copies may answer one page; the oldest says how out of date the screen can be.
            Set(true, SavedAt is { } shown && shown < saved ? shown : saved);
            return;
        }

        Set(false, null);
    }

    /// <summary>A call that reached nothing: offline, keeping the time of any saved copy already on screen.</summary>
    public void Unreachable() => Set(true, SavedAt);

    private void Set(bool offline, DateTimeOffset? savedAt)
    {
        if (offline == IsOffline && savedAt == SavedAt)
        {
            return;
        }

        IsOffline = offline;
        SavedAt = savedAt;
        Changed?.Invoke();
    }

    /// <summary>
    /// When a saved copy is from, for the banner: "at 2:14 PM" today, "yesterday at 2:14 PM", or "on Nov 26 at 2:14 PM".
    /// </summary>
    public static string Describe(DateTimeOffset savedAt, DateTimeOffset now, TimeZoneInfo zone)
    {
        var saved = TimeZoneInfo.ConvertTime(savedAt, zone);
        var today = TimeZoneInfo.ConvertTime(now, zone).Date;
        var time = saved.ToString("h:mm tt", CultureInfo.InvariantCulture);
        return (today - saved.Date).Days switch
        {
            <= 0 => $"at {time}",
            1 => $"yesterday at {time}",
            _ when saved.Year == today.Year => $"on {saved.ToString("MMM d", CultureInfo.InvariantCulture)} at {time}",
            _ => $"on {saved.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)} at {time}",
        };
    }
}
