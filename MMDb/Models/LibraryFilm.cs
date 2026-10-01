namespace MMDb.Models;

/// <summary>
/// A film in my Jellyfin library that I have not rated yet, with its TMDb details cached by the nightly job.
/// </summary>
public class LibraryFilm
{
    /// <summary>
    /// The TMDb movie ID, used as the primary key.
    /// </summary>
    public int TMDbId { get; set; }

    /// <summary>
    /// The IMDb ID, used to look up cached OMDb ratings.
    /// </summary>
    public string? IMDbId { get; set; }

    /// <summary>
    /// The film's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The release year according to TMDb.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The director or directors, comma separated.
    /// </summary>
    public string? Director { get; set; }

    /// <summary>
    /// The TMDb poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The average TMDb user rating, out of 10.
    /// </summary>
    public double? TMDbRating { get; set; }

    /// <summary>
    /// When the TMDb details were last fetched.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Builds an unsaved film for display, with any cached OMDb ratings applied.
    /// </summary>
    /// <param name="ratings">The cached OMDb ratings, or null if there are none.</param>
    public Film ToFilm(OMDbCacheEntry? ratings)
    {
        Film film = new() { TMDbId = TMDbId, IMDbId = IMDbId, Title = Title, Year = Year, Director = Director, PosterPath = PosterPath, TMDbRating = TMDbRating };
        ratings?.ApplyTo(film);
        return film;
    }
}