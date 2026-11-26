namespace Trove.Core.Playthroughs;

/// <summary>What changes when a video moves or is rated, kept apart from storage so it can be tested on its own.</summary>
public static class PlaythroughRules
{
    /// <summary>
    /// Moving to Done records today as the day watched; moving out of Done clears it. A rating can come with the move
    /// only into In progress or Done; moving elsewhere keeps any earlier rating, which shows again if you come back.
    /// </summary>
    public static (WatchState State, decimal? Rating, DateOnly? WatchedOn) Move(
        WatchState from, decimal? rating, DateOnly? watchedOn, MoveRequest request, DateOnly today)
    {
        if (!Enum.IsDefined(request.State))
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.State, "Unknown state.");
        }

        if (request.Rating is not null && !request.State.CanRate())
        {
            throw new ArgumentException("A rating can only be given in progress or done.", nameof(request));
        }

        var newRating = StarRating.Validate(request.Rating) ?? rating;
        DateOnly? newWatchedOn = request.State switch
        {
            WatchState.Done when from == WatchState.Done => watchedOn ?? today,
            WatchState.Done => today,
            _ => null,
        };

        return (request.State, newRating, newWatchedOn);
    }

    /// <summary>Sets or clears the rating; only in progress or done.</summary>
    public static decimal? Rate(WatchState state, decimal? rating) =>
        state.CanRate()
            ? StarRating.Validate(rating)
            : throw new InvalidOperationException("Start watching a video before rating it.");
}
