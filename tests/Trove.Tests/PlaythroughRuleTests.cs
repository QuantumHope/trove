using Trove.Core.Playthroughs;
using Trove.UI.Playthroughs;

namespace Trove.Tests;

public sealed class PlaythroughRuleTests
{
    private static readonly DateOnly Today = new(2026, 11, 26);

    // Real MKIceAndFire titles (web search, Nov 26, 2026). The channel puts FULL GAME on Part 1 of a series too.
    [Theory]
    [InlineData("UNCHARTED 1 DRAKE'S FORTUNE Gameplay Walkthrough FULL GAME [4K 60FPS PS5] - No Commentary", true)]
    [InlineData("STARFIELD Gameplay Walkthrough Part 1 FULL GAME [4K 60FPS] - No Commentary", false)]
    [InlineData("EVIL WEST Gameplay Walkthrough Part 1 FULL GAME [4K 60FPS PC ULTRA] - No Commentary", false)]
    [InlineData("THE CALLISTO PROTOCOL Gameplay Walkthrough Part 1 FULL GAME [4K 60FPS PS5] - No Commentary", false)]
    [InlineData("DEAD SPACE REMAKE Gameplay Walkthrough Part 1 FULL GAME [4K 60FPS] - No Commentary", false)]
    [InlineData("ATLAS FALLEN Gameplay Walkthrough Part 1 [4K 60FPS PC ULTRA] - No Commentary (FULL GAME)", false)]
    [InlineData("SOME GAME Gameplay Walkthrough Part 12 FULL GAME", false)]
    [InlineData("SOME GAME Walkthrough PART II Full Game", false)]
    [InlineData("SOME GAME Gameplay Walkthrough Full Game - No Commentary", true)]
    [InlineData("SOME GAME Gameplay Trailer 4K", false)]
    [InlineData("SOME GAME Party Mode FULL GAME", true)]
    [InlineData("THE LAST OF US PART I Gameplay Walkthrough FULL GAME [4K 60FPS PS5] - No Commentary", true)]
    [InlineData("THE LAST OF US PART 2 REMASTERED Gameplay Walkthrough FULL GAME - No Commentary", true)]
    [InlineData("THE LAST OF US PART 2 REMASTERED Gameplay Walkthrough Part 3 FULL GAME - No Commentary", false)]
    public void Full_game_filter_keeps_single_video_playthroughs_only(string title, bool kept) =>
        Assert.Equal(kept, FullGameFilter.IsFullGame(title));

    [Theory]
    [InlineData("0.25", true)]
    [InlineData("3.75", true)]
    [InlineData("5", true)]
    [InlineData("0", false)]
    [InlineData("3.3", false)]
    [InlineData("5.25", false)]
    public void Ratings_are_quarter_stars_from_a_quarter_to_five(string rating, bool valid) =>
        Assert.Equal(valid, StarRating.IsValid(decimal.Parse(rating, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Moving_to_done_records_today_and_the_rating()
    {
        var (state, rating, watched) = PlaythroughRules.Move(WatchState.InProgress, null, null, new MoveRequest(WatchState.Done, 4.25m), Today);

        Assert.Equal((WatchState.Done, 4.25m, Today), (state, rating, watched));
    }

    [Fact]
    public void Skipping_the_rating_keeps_one_given_while_in_progress()
    {
        var (_, rating, _) = PlaythroughRules.Move(WatchState.InProgress, 3.5m, null, new MoveRequest(WatchState.Done, null), Today);

        Assert.Equal(3.5m, rating);
    }

    [Fact]
    public void Moving_out_of_done_clears_the_watched_date()
    {
        var (_, rating, watched) = PlaythroughRules.Move(WatchState.Done, 4m, Today, new MoveRequest(WatchState.WatchNext, null), Today);

        Assert.Null(watched);
        Assert.Equal(4m, rating);
    }

    [Fact]
    public void Ratings_are_refused_before_watching_starts()
    {
        Assert.Throws<ArgumentException>(() => PlaythroughRules.Move(WatchState.New, null, null, new MoveRequest(WatchState.WatchNext, 3m), Today));
        Assert.Throws<InvalidOperationException>(() => PlaythroughRules.Rate(WatchState.Ignored, 3m));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlaythroughRules.Rate(WatchState.Done, 3.1m));
    }

    [Theory]
    [InlineData(1, "0.25")]
    [InlineData(6, "0.25")]
    [InlineData(7, "0.5")]
    [InlineData(60, "2.5")]
    [InlineData(66, "2.75")]
    [InlineData(120, "5")]
    public void A_click_rates_the_quarter_star_it_lands_in(double offsetX, string expected) =>
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), StarRatingInput.FromOffset(offsetX));

    [Theory]
    [InlineData(22331, "6:12:11")]
    [InlineData(2712, "45:12")]
    [InlineData(65, "1:05")]
    public void Lengths_read_like_youtube(int seconds, string expected) =>
        Assert.Equal(expected, VideoFormat.Length(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(950L, "950 views")]
    [InlineData(1_234_567L, "1.2M views")]
    [InlineData(45_600L, "45K views")]
    public void Views_read_like_youtube(long views, string expected) =>
        Assert.Equal(expected, VideoFormat.Views(views));
}
