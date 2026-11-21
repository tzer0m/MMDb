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
    /// The score, from just above 0 to 100; seeded from the original rating until comparisons settle it.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Whether enough comparisons have pinned the score down for it to be shown and compared against.
    /// </summary>
    public bool IsSettled { get; set; }

    /// <summary>
    /// Whether the film is an anchor I picked to bootstrap its band.
    /// </summary>
    public bool IsAnchor { get; set; }

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
    /// The score as a whole number from 1 to 100, as it is shown.
    /// </summary>
    [NotMapped]
    public int DisplayScore => (int)Math.Clamp(Math.Round(Score, MidpointRounding.AwayFromZero), 1, 100);

    /// <summary>
    /// The 1 to 10 band the score falls in, matching the original rating scale.
    /// </summary>
    [NotMapped]
    public int Band => (DisplayScore + 9) / 10;
}