namespace MMDb.Models.TMDb;

/// <summary>
/// A page of results from the TMDb movie search endpoint.
/// </summary>
public class TMDbSearchResponse
{
    /// <summary>
    /// The matching movies.
    /// </summary>
    public List<TMDbSearchResult> Results { get; set; } = [];
}