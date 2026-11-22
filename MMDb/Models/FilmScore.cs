using System.ComponentModel.DataAnnotations.Schema;

namespace MMDb.Models;

/// <summary>
/// A film's 1 to 100 score from comparisons, kept separate from its original 1 to 10 rating.
/// </summary>
public class FilmScore
{
    /// <summary>
    /// The film this score is for; also the primary key.
    /// </summary>
    public int FilmId { get; set; }

    /// <summary>
    /// The film this score is for.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The raw score, from just above 0 to 100, which only orders films for ranking.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// The score that is shown, from 0 to 100; it stays fixed once set, and a newly settled film gets the midpoint of the films either side of it; null until settled.
    /// </summary>
    public double? DisplayScore { get; set; }

    /// <summary>
    /// Whether enough comparisons have pinned the score down for it to be shown and compared against.
    /// </summary>
    public bool IsSettled { get; set; }

    /// <summary>
    /// The film this one shares its score with, e.g. another cut of the same film; linked films are never ranked themselves.
    /// </summary>
    public int? LinkedToFilmId { get; set; }

    /// <summary>
    /// The film this one shares its score with.
    /// </summary>
    public Film? LinkedToFilm { get; set; }

    /// <summary>
    /// When the current placement started (UTC); only comparisons after this count towards it.
    /// </summary>
    public DateTime ResetAt { get; set; }

    /// <summary>
    /// When the score was last settled (UTC).
    /// </summary>
    public DateTime? SettledAt { get; set; }

    /// <summary>
    /// When the score was last changed (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// The shown score as a whole number from 1 to 100, or null until settled.
    /// </summary>
    [NotMapped]
    public int? ShownScore => DisplayScore is double displayScore ? DisplayFor(displayScore) : null;

    /// <summary>
    /// Returns a score as the whole number from 1 to 100 that is shown.
    /// </summary>
    /// <param name="score">The score.</param>
    public static int DisplayFor(double score)
    {
        return (int)Math.Clamp(Math.Round(score, MidpointRounding.AwayFromZero), 1, 100);
    }
}