namespace MMDb.Models.TMDb;

/// <summary>
/// A TV show's IDs on other sites, from TMDb.
/// </summary>
public class TMDbExternalIds
{
    /// <summary>
    /// The IMDb ID, e.g. tt0903747.
    /// </summary>
    public string? ImdbId { get; set; }
}