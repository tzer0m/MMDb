namespace MMDb.Models;

/// <summary>
/// One answer from ranking a film: whether it was better, worse or similar to another film.
/// </summary>
public class FilmComparison
{
    /// <summary>
    /// The primary key.
    /// </summary>
    public int FilmComparisonId { get; set; }

    /// <summary>
    /// The ID of the film being ranked.
    /// </summary>
    public int FilmId { get; set; }

    /// <summary>
    /// The film being ranked.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The ID of the film it was compared with.
    /// </summary>
    public int OtherFilmId { get; set; }

    /// <summary>
    /// The film it was compared with.
    /// </summary>
    public Film OtherFilm { get; set; } = null!;

    /// <summary>
    /// My answer.
    /// </summary>
    public ComparisonResult Result { get; set; }

    /// <summary>
    /// When I answered (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
}