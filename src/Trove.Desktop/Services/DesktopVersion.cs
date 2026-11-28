using System.Reflection;

namespace Trove.Desktop.Services;

public static class DesktopVersion
{
    /// <summary>The window's version, without any build metadata after a plus sign.</summary>
    public static string Current { get; } =
        (typeof(DesktopVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];
}
