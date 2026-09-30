namespace MMDb.Models.TMDb;

/// <summary>
/// Full details of a movie from TMDb, including credits.
/// </summary>
public class TMDbMovie
{
    /// <summary>
    /// The TMDb movie ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The IMDb ID, e.g. tt0113277.
    /// </summary>
    public string? ImdbId { get; set; }

    /// <summary>
    /// The movie's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The release date as yyyy-MM-dd, or empty if unknown.
    /// </summary>
    public string? ReleaseDate { get; set; }

    /// <summary>
    /// The runtime in minutes.
    /// </summary>
    public int? Runtime { get; set; }

    /// <summary>
    /// The movie's tagline.
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// A short synopsis of the movie.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// The average TMDb user rating, out of 10.
    /// </summary>
    public double VoteAverage { get; set; }

    /// <summary>
    /// The number of TMDb user ratings.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The backdrop image path.
    /// </summary>
    public string? BackdropPath { get; set; }

    /// <summary>
    /// The cast and crew, when requested with append_to_response=credits.
    /// </summary>
    public TMDbCredits? Credits { get; set; }

    /// <summary>
    /// The release year, parsed from the release date.
    /// </summary>
    public int? Year => ReleaseDate is { Length: >= 4 } && int.TryParse(ReleaseDate[..4], out int year) ? year : null;
}