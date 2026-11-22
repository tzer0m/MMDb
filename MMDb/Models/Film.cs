using System.ComponentModel.DataAnnotations.Schema;
using MMDb.Helpers;

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
    /// My original rating, from 1 to 10, from before films were ranked by comparison.
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
    /// The 1 to 100 score from comparisons, if it has been seeded.
    /// </summary>
    public FilmScore? FilmScore { get; set; }

    /// <summary>
    /// The average of the TMDb, IMDb, Rotten Tomatoes and Metacritic ratings on a 10-point scale, using whichever are available.
    /// </summary>
    [NotMapped]
    public double? CommunityRating => CommunityRatingCalculator.Calculate(TMDbRating, IMDbRating, RottenTomatoes, Metacritic);

    /// <summary>
    /// My rating minus the community rating, both out of 10, using the original rating stretched onto the same scale until the score is settled: positive if I rate it higher, negative if the community does.
    /// </summary>
    [NotMapped]
    public double? RatingDelta => CommunityRating is double communityRating ? Math.Round((MyScore ?? RatingScale.FromOneToTen(Rating)) - communityRating, 1) : null;

    /// <summary>
    /// My score out of 10 to one decimal place, from the settled 1 to 100 score, or null until it is settled.
    /// </summary>
    [NotMapped]
    public double? MyScore => FilmScore?.ShownScore is int shown ? shown / 10.0 : null;

    /// <summary>
    /// My rating as shown: the score out of 10 once settled, otherwise the original rating.
    /// </summary>
    [NotMapped]
    public double MyRating => MyScore ?? Rating;

    /// <summary>
    /// My rating formatted for display, with one decimal place once settled.
    /// </summary>
    [NotMapped]
    public string MyRatingText => MyScore is double score ? score.ToString("0.0") : Rating.ToString();

    /// <summary>
    /// The tooltip for my rating, mentioning the original rating.
    /// </summary>
    [NotMapped]
    public string MyRatingTitle => MyScore is null ? "My Rating (not ranked yet)" : $"My Rating (originally {Rating})";

    /// <summary>
    /// The critics rating pushed to Jellyfin, out of 100: the score once settled, otherwise the original rating times 10.
    /// </summary>
    [NotMapped]
    public int CriticRating => FilmScore?.ShownScore ?? Rating * 10;

    /// <summary>
    /// The runtime formatted as hours and minutes.
    /// </summary>
    [NotMapped]
    public string? RuntimeText => RuntimeMinutes is int minutes ? $"{minutes / 60}h {minutes % 60}m" : null;
}