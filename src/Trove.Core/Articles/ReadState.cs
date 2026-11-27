using System.Text.Json.Serialization;

namespace Trove.Core.Articles;

/// <summary>Where you are with an article. A link you save lands in <see cref="ReadNext"/>; feed articles land in <see cref="New"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReadState>))]
public enum ReadState
{
    New,
    ReadNext,
    Reading,
    Done,
    Ignored,
}

public static class ReadStates
{
    /// <summary>The board's columns, in order. Ignored articles are kept out of the way.</summary>
    public static IReadOnlyList<ReadState> Columns { get; } = [ReadState.New, ReadState.ReadNext, ReadState.Reading, ReadState.Done];

    public static IReadOnlyList<ReadState> All { get; } = [.. Columns, ReadState.Ignored];

    public static string Label(this ReadState state) => state switch
    {
        ReadState.New => "New",
        ReadState.ReadNext => "Read next",
        ReadState.Reading => "Reading",
        ReadState.Done => "Done",
        ReadState.Ignored => "Ignored",
        _ => state.ToString(),
    };

    /// <summary>The card's one-tap move: the next step along the list, or null when it's finished.</summary>
    public static (ReadState To, string Label)? Forward(this ReadState state) => state switch
    {
        ReadState.New => (ReadState.ReadNext, "Read next"),
        ReadState.ReadNext => (ReadState.Reading, "Start reading"),
        ReadState.Reading => (ReadState.Done, "Mark done"),
        ReadState.Ignored => (ReadState.New, "Restore"),
        _ => null,
    };
}
