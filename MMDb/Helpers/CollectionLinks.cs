using MMDb.Models;
using MMDb.Models.Jellyfin;
using MMDb.Services;

namespace MMDb.Helpers;

/// <summary>
/// Finds the Jellyfin collections to link to from a film's header.
/// </summary>
public static class CollectionLinks
{
    /// <summary>
    /// Returns the collections the film is in, or none if Jellyfin cannot be reached.
    /// </summary>
    /// <param name="jellyfin">The Jellyfin client.</param>
    /// <param name="film">The film being shown.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<List<JellyfinCollection>> FindAsync(JellyfinClient jellyfin, Film film, CancellationToken cancellationToken)
    {
        try
        {
            return await jellyfin.FindCollectionsAsync(film.IMDbId, film.TMDbId, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}