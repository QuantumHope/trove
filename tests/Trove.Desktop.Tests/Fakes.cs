using Trove.Desktop.Services;

namespace Trove.Desktop.Tests;

internal sealed class FakeProbe(ProbeResult result = ProbeResult.Ready) : ICoraProbe
{
    public ProbeResult Result { get; set; } = result;

    public List<string> Checked { get; } = [];

    public Task<ProbeResult> CheckAsync(string origin, CancellationToken cancellationToken = default)
    {
        Checked.Add(origin);
        return Task.FromResult(Result);
    }
}

/// <summary>A folder of its own for one test, deleted afterwards.</summary>
internal sealed class TempHome : IDisposable
{
    public TempHome() => Directory.CreateDirectory(Home.Root);

    public TroveHome Home { get; } = new(Path.Combine(Path.GetTempPath(), "trove-desktop-tests", Guid.NewGuid().ToString("N")));

    public DesktopSettings Settings => new(Home.SettingsFile);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Home.Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
