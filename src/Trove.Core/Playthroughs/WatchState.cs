using System.Text.Json.Serialization;

namespace Trove.Core.Playthroughs;

/// <summary>Where you are with a playthrough video. New videos land in <see cref="New"/> until you sort them.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<WatchState>))]
public enum WatchState
{
    New,
    WatchNext,
    InProgress,
    Done,
    Ignored,
}

public static class WatchStates
{
    /// <summary>The board's columns, in order. Ignored videos are kept out of the way.</summary>
    public static IReadOnlyList<WatchState> Columns { get; } = [WatchState.New, WatchState.WatchNext, WatchState.InProgress, WatchState.Done];

    public static IReadOnlyList<WatchState> All { get; } = [.. Columns, WatchState.Ignored];

    public static string Label(this WatchState state) => state switch
    {
        WatchState.New => "New",
        WatchState.WatchNext => "Watch next",
        WatchState.InProgress => "In progress",
        WatchState.Done => "Done",
        WatchState.Ignored => "Ignored",
        _ => state.ToString(),
    };

    /// <summary>A rating can be given or changed only once you've started watching.</summary>
    public static bool CanRate(this WatchState state) => state is WatchState.InProgress or WatchState.Done;
}
