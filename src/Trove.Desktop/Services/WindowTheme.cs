using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Lab = MattEland.HopeLab.Themes;

namespace Trove.Desktop.Services;

/// <summary>
/// Dresses the window's own screens in the theme Trove's page uses. The page keeps the choice ("system" or a shared
/// HopeLab theme id) and the window reads it after each load, so the connect and offline screens match the board.
/// "system" follows the computer between the shared Light and Dark themes, as Trove does.
/// </summary>
public static class WindowTheme
{
    /// <summary>A key only Trove adds, to find the theme's dictionary again when it changes.</summary>
    public const string MarkerKey = "Trove.Desktop.Theme.Id";

    public static Lab.ThemeInfo Resolve(string? preference, bool systemDark) =>
        preference is { Length: > 0 } id && id != "system" && Lab.ThemeCatalog.Find(id) is { } chosen
            ? chosen
            : Lab.ThemeCatalog.FindOrDefault(systemDark ? "dark" : "light");

    public static void Apply(Application app, string? preference)
    {
        var systemDark = app.PlatformSettings?.GetColorValues().ThemeVariant != PlatformThemeVariant.Light;
        var theme = Resolve(preference, systemDark);

        var merged = app.Resources.MergedDictionaries;
        for (var i = merged.Count - 1; i >= 0; i--)
        {
            if (merged[i].TryGetResource(MarkerKey, null, out _))
            {
                merged.RemoveAt(i);
            }
        }

        var resources = theme.CreateResources();
        resources[MarkerKey] = theme.Id;
        merged.Add(resources);

        var variant = theme.IsLight ? ThemeVariant.Light : ThemeVariant.Dark;
        app.RequestedThemeVariant = variant;
        if (app.Styles.OfType<FluentTheme>().FirstOrDefault() is { } fluent)
        {
            if (!fluent.Palettes.TryGetValue(variant, out var p))
            {
                fluent.Palettes[variant] = p = new ColorPaletteResources();
            }

            Color C(string key) => ColorOf(resources, key);
            p.Accent = C("Lab.Accent.Primary");
            p.AltHigh = C("Lab.Surface.Canvas");
            p.RegionColor = C("Lab.Surface.Canvas");
            p.BaseHigh = C("Lab.Text.Primary");
            p.ChromeMediumLow = C("Lab.Surface.Panel");
            p.ChromeMedium = C("Lab.Surface.Elevated");
        }
    }

    public static Color ColorOf(IResourceProvider resources, string key) =>
        resources.TryGetResource(key + ".Color", null, out var c) && c is Color color ? color
        : resources.TryGetResource(key, null, out var b) && b is ISolidColorBrush brush ? brush.Color
        : Colors.Gray;
}
