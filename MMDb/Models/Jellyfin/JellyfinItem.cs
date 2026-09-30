namespace MMDb.Models.Jellyfin;

/// <summary>
/// A movie in the Jellyfin library.
/// </summary>
public class JellyfinItem
{
    /// <summary>
    /// The Jellyfin item ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The movie's title.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The year the movie was released.
    /// </summary>
    public int? ProductionYear { get; set; }

    /// <summary>
    /// The community rating, which holds my rating.
    /// </summary>
    public double? CommunityRating { get; set; }

    /// <summary>
    /// External IDs keyed by provider, e.g. Imdb and Tmdb.
    /// </summary>
    public Dictionary<string, string> ProviderIds { get; set; } = [];

    /// <summary>
    /// The IMDb ID, if known.
    /// </summary>
    public string? IMDbId => ProviderIds.FirstOrDefault(x => x.Key.Equals("Imdb", StringComparison.OrdinalIgnoreCase)).Value;

    /// <summary>
    /// The TMDb ID, if known.
    /// </summary>
    public int? TMDbId => int.TryParse(ProviderIds.FirstOrDefault(x => x.Key.Equals("Tmdb", StringComparison.OrdinalIgnoreCase)).Value, out int id) ? id : null;
}