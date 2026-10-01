namespace MMDb.Models.Jellyfin;

/// <summary>
/// A Jellyfin collection (box set) and the movies in it.
/// </summary>
public class JellyfinCollection
{
    /// <summary>
    /// The Jellyfin item ID of the collection.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The collection name, e.g. Harry Potter Collection.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The movies in the collection.
    /// </summary>
    public List<JellyfinItem> Movies { get; set; } = [];

    /// <summary>
    /// Checks whether a film is in this collection, by IMDb ID or TMDb ID.
    /// </summary>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="tmdbId">The TMDb ID.</param>
    public bool Contains(string? imdbId, int? tmdbId)
    {
        return Movies.Any(x => (imdbId is not null && string.Equals(x.IMDbId, imdbId, StringComparison.OrdinalIgnoreCase)) || (tmdbId is not null && x.TMDbId == tmdbId));
    }
}