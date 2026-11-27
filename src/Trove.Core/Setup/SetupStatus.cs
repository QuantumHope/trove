using System.Text.Json.Serialization;

namespace Trove.Core.Setup;

/// <summary>
/// What CORA's Trove module has of what each board needs (CORA ADR-131). Whether each setting is there, never its value.
/// <paramref name="CanSetCoraAddress"/> is whether the person signed in is CORA's admin, who alone can save the address;
/// an older module that doesn't send it reads as no.
/// </summary>
public sealed record SetupStatus(bool CoraAddressSet, bool YouTubeKeySet, int Channels, bool CanSetCoraAddress = false);

[JsonConverter(typeof(JsonStringEnumConverter<CoraAddressSource>))]
public enum CoraAddressSource
{
    None,
    Configuration,
    Saved,
}

/// <summary>Where the module finds CORA, as the module answers after a save.</summary>
public sealed record CoraAddress(string? BaseUrl, CoraAddressSource Source, string? ConfiguredBaseUrl);

public sealed record SaveCoraAddressResult(bool Saved, string? Error, CoraAddress? Address);
