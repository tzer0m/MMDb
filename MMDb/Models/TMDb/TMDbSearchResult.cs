namespace MMDb.Models.TMDb;

/// <summary>
/// A single movie from a TMDb search.
/// </summary>
public class TMDbSearchResult
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
    /// A short synopsis of the movie.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// The release year, parsed from the release date.
    /// </summary>
    public int? Year => ReleaseDate is { Length: >= 4 } && int.TryParse(ReleaseDate[..4], out int year) ? year : null;
}