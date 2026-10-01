namespace MMDb.Models;

/// <summary>
/// One of a person's top rated films on TMDb, whether or not I have seen it.
/// </summary>
public class TopRatedFilm
{
    /// <summary>
    /// The TMDb movie ID.
    /// </summary>
    public int TMDbId { get; set; }

    /// <summary>
    /// The film's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The release year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The TMDb poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The average TMDb user rating, out of 10.
    /// </summary>
    public double TMDbRating { get; set; }

    /// <summary>
    /// The community rating, falling back to the TMDb rating alone when OMDb has nothing.
    /// </summary>
    public double? CommunityRating { get; set; }

    /// <summary>
    /// Which ratings went into the community rating, shown as the badge tooltip.
    /// </summary>
    public string RatingSources { get; set; } = string.Empty;

    /// <summary>
    /// Their role: Director, the character they played, or both.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// My film ID if I have rated it, otherwise null.
    /// </summary>
    public int? FilmId { get; set; }

    /// <summary>
    /// Whether the film is in my Jellyfin library.
    /// </summary>
    public bool InLibrary { get; set; }
}