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
    /// My rating, from 1 to 10.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// My written review.
    /// </summary>
    public string? Review { get; set; }

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
    /// The runtime formatted as hours and minutes.
    /// </summary>
    [NotMapped]
    public string? RuntimeText => RuntimeMinutes is int minutes ? $"{minutes / 60}h {minutes % 60}m" : null;
}