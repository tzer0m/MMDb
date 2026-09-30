namespace MMDb.Helpers;

/// <summary>
/// Works out the community rating shared by films, previews and search results.
/// </summary>
public static class CommunityRatingCalculator
{
    /// <summary>
    /// Averages whichever of the TMDb, IMDb, Rotten Tomatoes and Metacritic ratings are available on a 10-point scale, returning null if there are none.
    /// </summary>
    /// <param name="tmdbRating">The TMDb rating, out of 10.</param>
    /// <param name="imdbRating">The IMDb rating, out of 10.</param>
    /// <param name="rottenTomatoes">The Rotten Tomatoes score, as a percentage.</param>
    /// <param name="metacritic">The Metacritic score, out of 100.</param>
    public static double? Calculate(double? tmdbRating, double? imdbRating, int? rottenTomatoes, int? metacritic)
    {
        List<double> ratings = [];
        if (tmdbRating is double tmdb)
        {
            ratings.Add(tmdb);
        }
        if (imdbRating is double imdb)
        {
            ratings.Add(imdb);
        }
        if (rottenTomatoes is int rottenTomatoesScore)
        {
            ratings.Add(rottenTomatoesScore / 10.0);
        }
        if (metacritic is int metacriticScore)
        {
            ratings.Add(metacriticScore / 10.0);
        }
        return ratings.Count == 0 ? null : Math.Round(ratings.Average(), 1);
    }

    /// <summary>
    /// Describes which ratings went into the community rating, for a badge tooltip.
    /// </summary>
    /// <param name="tmdbRating">The TMDb rating, out of 10.</param>
    /// <param name="imdbRating">The IMDb rating, out of 10.</param>
    /// <param name="rottenTomatoes">The Rotten Tomatoes score, as a percentage.</param>
    /// <param name="metacritic">The Metacritic score, out of 100.</param>
    public static string Describe(double? tmdbRating, double? imdbRating, int? rottenTomatoes, int? metacritic)
    {
        List<string> sources = [];
        if (tmdbRating is not null)
        {
            sources.Add("TMDb");
        }
        if (imdbRating is not null)
        {
            sources.Add("IMDb");
        }
        if (rottenTomatoes is not null)
        {
            sources.Add("Rotten Tomatoes");
        }
        if (metacritic is not null)
        {
            sources.Add("Metacritic");
        }
        return sources.Count == 1 ? $"{sources[0]} Rating" : $"Community Rating ({string.Join(", ", sources)})";
    }
}