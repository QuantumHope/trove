using System.Text.Json.Serialization;

namespace Trove.Core.Setup;

/// <summary>
/// What CORA's Trove module has of what each board needs (CORA ADR-131). Whether each setting is there, never its value.
/// </summary>
public sealed record SetupStatus(bool CoraAddressSet, bool YouTubeKeySet, int Channels);

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
