namespace MMDb.Models;

/// <summary>
/// A cached copy of a film's OMDb ratings, used by searches and previews to save OMDb requests.
/// </summary>
public class OMDbCacheEntry
{
    /// <summary>
    /// The IMDb ID, used as the primary key.
    /// </summary>
    public string IMDbId { get; set; } = string.Empty;

    /// <summary>
    /// The release year according to IMDb.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The IMDb rating, out of 10.
    /// </summary>
    public double? IMDbRating { get; set; }

    /// <summary>
    /// The number of IMDb votes.
    /// </summary>
    public int? IMDbVotes { get; set; }

    /// <summary>
    /// The Rotten Tomatoes score, as a percentage.
    /// </summary>
    public int? RottenTomatoes { get; set; }

    /// <summary>
    /// The Metacritic score, out of 100.
    /// </summary>
    public int? Metacritic { get; set; }

    /// <summary>
    /// When the ratings were fetched from OMDb (UTC).
    /// </summary>
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// Copies these ratings, and the IMDb year if known, onto a film.
    /// </summary>
    /// <param name="film">The film to update.</param>
    public void ApplyTo(Film film)
    {
        film.Year = Year ?? film.Year;
        film.IMDbRating = IMDbRating;
        film.IMDbVotes = IMDbVotes;
        film.RottenTomatoes = RottenTomatoes;
        film.Metacritic = Metacritic;
    }
}