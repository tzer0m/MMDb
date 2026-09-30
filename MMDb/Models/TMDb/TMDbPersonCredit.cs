namespace MMDb.Models.TMDb;

/// <summary>
/// A film a person acted in or worked on, from TMDb's person movie credits.
/// </summary>
public class TMDbPersonCredit
{
    /// <summary>
    /// The TMDb movie ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The movie's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The release date as yyyy-MM-dd, or empty if unknown.
    /// </summary>
    public string? ReleaseDate { get; set; }

    /// <summary>
    /// The poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The average TMDb user rating, out of 10.
    /// </summary>
    public double VoteAverage { get; set; }

    /// <summary>
    /// The number of TMDb user ratings.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The character played, for cast credits.
    /// </summary>
    public string? Character { get; set; }

    /// <summary>
    /// The job, for crew credits, e.g. Director.
    /// </summary>
    public string? Job { get; set; }

    /// <summary>
    /// The release year, parsed from the release date.
    /// </summary>
    public int? Year => ReleaseDate is { Length: >= 4 } && int.TryParse(ReleaseDate[..4], out int year) ? year : null;
}