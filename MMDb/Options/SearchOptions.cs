namespace MMDb.Options;

/// <summary>
/// Configuration for the film lookup page.
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// The most TMDb results to show for a search.
    /// </summary>
    public int MaxResults { get; set; } = 10;
}