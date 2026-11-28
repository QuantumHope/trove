using System.Text.Json;
using System.Text.Json.Nodes;
using MattEland.HopeLab.Common;

namespace Trove.Desktop.Services;

/// <summary>What the window remembers between starts.</summary>
/// <param name="CoraAddress">CORA's origin, such as https://cora.example.ts.net; null until the first connect.</param>
/// <param name="Theme">Trove's theme choice as the page last reported it ("system" or a theme id).</param>
/// <param name="OpenedTrove">True once Trove has loaded in this window, so its service worker holds a saved copy.</param>
public sealed record DesktopPreferences(string? CoraAddress = null, string Theme = "system", bool OpenedTrove = false);

/// <summary>
/// Reads and writes desktop.json. A missing or unreadable file reads as a first start; writes keep any keys this
/// version doesn't know, so an older and a newer window can share the folder.
/// </summary>
public sealed class DesktopSettings(string path)
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public string Path { get; } = System.IO.Path.GetFullPath(path);

    public DesktopPreferences Load()
    {
        var doc = Read();
        return new DesktopPreferences(
            Text(doc, "coraAddress") is { } address && CoraAddress.TryParse(address, out var origin, out _) ? origin : null,
            Text(doc, "theme") ?? "system",
            doc["openedTrove"] is JsonValue v && v.TryGetValue<bool>(out var opened) && opened);
    }

    public async Task SaveAsync(DesktopPreferences preferences)
    {
        var doc = Read();
        doc["coraAddress"] = preferences.CoraAddress;
        doc["theme"] = preferences.Theme;
        doc["openedTrove"] = preferences.OpenedTrove;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        await AtomicFile.WriteAllTextAsync(Path, doc.ToJsonString(Indented));
    }

    private JsonObject Read()
    {
        try
        {
            return File.Exists(Path) && JsonNode.Parse(File.ReadAllText(Path)) is JsonObject o ? o : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return [];
        }
    }

    private static string? Text(JsonObject doc, string key) =>
        doc[key] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s : null;
}
