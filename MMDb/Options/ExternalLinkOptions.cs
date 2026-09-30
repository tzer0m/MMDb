namespace MMDb.Options;

/// <summary>
/// URL templates for linking to a film on external sites, with {0} replaced by the film's ID or title.
/// </summary>
public class ExternalLinkOptions
{
    /// <summary>
    /// The TMDb film page, with {0} as the TMDb ID.
    /// </summary>
    public string TMDb { get; set; } = string.Empty;

    /// <summary>
    /// The IMDb film page, with {0} as the IMDb ID.
    /// </summary>
    public string IMDb { get; set; } = string.Empty;

    /// <summary>
    /// The IMDb person page, with {0} as the person's IMDb ID.
    /// </summary>
    public string IMDbPerson { get; set; } = string.Empty;

    /// <summary>
    /// The Rotten Tomatoes search page, with {0} as the URL-encoded title.
    /// </summary>
    public string RottenTomatoes { get; set; } = string.Empty;

    /// <summary>
    /// The Metacritic search page, with {0} as the URL-encoded title.
    /// </summary>
    public string Metacritic { get; set; } = string.Empty;

    /// <summary>
    /// The site's GitHub repository.
    /// </summary>
    public string GitHub { get; set; } = string.Empty;

    /// <summary>
    /// The SwagBagger search page, with {0} as the URL-encoded title and year.
    /// </summary>
    public string SwagBagger { get; set; } = string.Empty;
}