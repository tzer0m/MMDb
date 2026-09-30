using System.Security.Claims;
using MMDb.Models;
using MMDb.Services;

namespace MMDb.Helpers;

/// <summary>
/// Decides whether to show the SwagBagger search button on a film.
/// </summary>
public static class SwagBaggerButton
{
    /// <summary>
    /// Returns true when I am signed in and the film is not in Jellyfin, or Jellyfin cannot be reached to check.
    /// </summary>
    /// <param name="user">The current user.</param>
    /// <param name="jellyfin">The Jellyfin client.</param>
    /// <param name="film">The film being shown.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task<bool> ShouldShowAsync(ClaimsPrincipal user, JellyfinClient jellyfin, Film film, CancellationToken cancellationToken)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }
        try
        {
            return !await jellyfin.IsInLibraryAsync(film.IMDbId, film.TMDbId, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return true;
        }
    }
}