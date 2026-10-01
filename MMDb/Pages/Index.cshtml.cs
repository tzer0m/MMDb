using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Helpers;
using MMDb.Models;

namespace MMDb.Pages;

/// <summary>
/// The home page, listing all rated films in a table, followed by unrated films in my Jellyfin library.
/// </summary>
/// <param name="db">The database context.</param>
public class IndexModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// All rated films, most recently watched first.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// Unrated films in my Jellyfin library, as unsaved films with any cached ratings applied; only loaded when signed in.
    /// </summary>
    public List<Film> Unwatched { get; set; } = [];

    /// <summary>
    /// How many films I rated 1 to 10, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] MyRatingCounts { get; set; } = [];

    /// <summary>
    /// How many films have each community rating, rounded to the nearest whole number, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] CommunityRatingCounts { get; set; } = [];

    /// <summary>
    /// Loads every film and counts the ratings for the charts; searching and sorting happen in the browser.
    /// </summary>
    public async Task OnGetAsync()
    {
        Films = await db.Films.AsNoTracking().OrderByDescending(x => x.WatchedOn).ThenBy(x => x.Title).ToListAsync();
        MyRatingCounts = RatingDistribution.ForMyRatings(Films);
        CommunityRatingCounts = RatingDistribution.ForCommunityRatings(Films);
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }
        HashSet<int> rated = [.. Films.Select(x => x.TMDbId).OfType<int>()];
        List<LibraryFilm> libraryFilms = [.. (await db.LibraryFilms.AsNoTracking().OrderBy(x => x.Title).ToListAsync()).Where(x => !rated.Contains(x.TMDbId))];
        List<string> imdbIds = [.. libraryFilms.Select(x => x.IMDbId).OfType<string>()];
        Dictionary<string, OMDbCacheEntry> ratings = await db.OMDbCache.AsNoTracking().Where(x => imdbIds.Contains(x.IMDbId)).ToDictionaryAsync(x => x.IMDbId, StringComparer.OrdinalIgnoreCase);
        Unwatched = [.. libraryFilms.Select(x => x.ToFilm(x.IMDbId is string imdbId ? ratings.GetValueOrDefault(imdbId) : null))];
    }
}