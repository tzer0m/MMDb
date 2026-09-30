using MMDb.Models;

namespace MMDb.Helpers;

/// <summary>
/// Counts how many films have each rating from 1 to 10, for the rating charts.
/// </summary>
public static class RatingDistribution
{
    /// <summary>
    /// Counts my ratings, indexed from 0 for a rating of 1.
    /// </summary>
    /// <param name="films">The films to count.</param>
    public static int[] ForMyRatings(IEnumerable<Film> films)
    {
        int[] counts = new int[10];
        foreach (Film film in films)
        {
            counts[Math.Clamp(film.Rating, 1, 10) - 1]++;
        }
        return counts;
    }

    /// <summary>
    /// Counts community ratings rounded to the nearest whole number, indexed from 0 for a rating of 1, skipping films without one.
    /// </summary>
    /// <param name="films">The films to count.</param>
    public static int[] ForCommunityRatings(IEnumerable<Film> films)
    {
        int[] counts = new int[10];
        foreach (Film film in films)
        {
            if (film.CommunityRating is double communityRating)
            {
                counts[Math.Clamp((int)Math.Round(communityRating, MidpointRounding.AwayFromZero), 1, 10) - 1]++;
            }
        }
        return counts;
    }
}