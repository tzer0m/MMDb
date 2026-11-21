namespace MMDb.Helpers;

/// <summary>
/// Converts ratings between scales so they can be compared fairly.
/// </summary>
public static class RatingScale
{
    /// <summary>
    /// Stretches a 1 to 10 rating onto the 0 to 10 scale the community rating uses, so 1 becomes 0 and 10 stays 10.
    /// </summary>
    /// <param name="rating">The rating, from 1 to 10.</param>
    public static double FromOneToTen(double rating)
    {
        return (rating - 1) * 10 / 9;
    }
}