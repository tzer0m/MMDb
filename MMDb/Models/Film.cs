using System.ComponentModel.DataAnnotations.Schema;

namespace MMDb.Models;

/// <summary>
/// A film and my rating of it.
/// </summary>
public class Film
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public int FilmId { get; set; }

    /// <summary>
    /// The TMDb movie ID.
    /// </summary>
    public int? TMDbId { get; set; }

    /// <summary>
    /// The IMDb ID, e.g. tt0113277.
    /// </summary>
    public string? IMDbId { get; set; }

    /// <summary>
    /// The film's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The year the film was released.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The film's director.
    /// </summary>
    public string? Director { get; set; }

    /// <summary>
    /// The film's runtime in minutes.
    /// </summary>
    public int? RuntimeMinutes { get; set; }

    /// <summary>
    /// A short synopsis of the film.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// The film's tagline.
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// The film's genres.
    /// </summary>
    public List<string> Genres { get; set; } = [];

    /// <summary>
    /// The top-billed cast, in billing order.
    /// </summary>
    public List<string> Cast { get; set; } = [];

    /// <summary>
    /// The TMDb poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The TMDb backdrop image path.
    /// </summary>
    public string? BackdropPath { get; set; }

    /// <summary>
    /// The average TMDb user rating, out of 10.
    /// </summary>
    public double? TMDbRating { get; set; }

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
    /// When the external ratings were last refreshed (UTC).
    /// </summary>
    public DateTime? RatingsUpdatedAt { get; set; }

    /// <summary>
    /// My rating, from 1 to 10.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// The date I watched the film.
    /// </summary>
    public DateOnly? WatchedOn { get; set; }

    /// <summary>
    /// When the film was added (UTC).
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// When the film was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The director and cast credits.
    /// </summary>
    public List<FilmCredit> Credits { get; set; } = [];

    /// <summary>
    /// The average of the TMDb, IMDb, Rotten Tomatoes and Metacritic ratings on a 10-point scale, using whichever are available.
    /// </summary>
    [NotMapped]
    public double? CommunityRating
    {
        get
        {
            List<double> ratings = [];
            if (TMDbRating is double tmdb)
            {
                ratings.Add(tmdb);
            }
            if (IMDbRating is double imdb)
            {
                ratings.Add(imdb);
            }
            if (RottenTomatoes is int rottenTomatoes)
            {
                ratings.Add(rottenTomatoes / 10.0);
            }
            if (Metacritic is int metacritic)
            {
                ratings.Add(metacritic / 10.0);
            }
            return ratings.Count == 0 ? null : Math.Round(ratings.Average(), 1);
        }
    }

    /// <summary>
    /// The runtime formatted as hours and minutes.
    /// </summary>
    [NotMapped]
    public string? RuntimeText => RuntimeMinutes is int minutes ? $"{minutes / 60}h {minutes % 60}m" : null;
}