namespace Trove.Core.Playthroughs;

/// <summary>Five-star ratings in quarter-star steps, from 0.25 to 5.</summary>
public static class StarRating
{
    public const decimal Step = 0.25m;
    public const decimal Max = 5m;

    public static bool IsValid(decimal rating) => rating is >= Step and <= Max && rating % Step == 0;

    /// <summary>Throws when <paramref name="rating"/> is set but isn't a quarter step between 0.25 and 5.</summary>
    public static decimal? Validate(decimal? rating) =>
        rating is { } value && !IsValid(value)
            ? throw new ArgumentOutOfRangeException(nameof(rating), value, "A rating is 0.25 to 5 stars, in quarter-star steps.")
            : rating;

    /// <summary>How full star <paramref name="index"/> (0 to 4) is for <paramref name="rating"/>, from 0 to 1.</summary>
    public static decimal Fill(decimal? rating, int index) => Math.Clamp((rating ?? 0) - index, 0, 1);

    public static string Describe(decimal? rating) => rating is { } value ? $"{value:0.##} of 5 stars" : "Not rated";
}
