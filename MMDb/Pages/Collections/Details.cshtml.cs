using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Models.Jellyfin;
using MMDb.Services;

namespace MMDb.Pages.Collections;

/// <summary>
/// Shows a Jellyfin collection with my average ratings; every film in it when signed in, otherwise just the ones I have seen.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="jellyfin">The Jellyfin client.</param>
public class DetailsModel(MMDbContext db, JellyfinClient jellyfin) : PageModel
{
    /// <summary>
    /// The collection name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The films in the collection, oldest first; unrated ones are unsaved films with a film ID of 0.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// My average rating across the films I have rated.
    /// </summary>
    public double? AverageMyRating => Films.Any(x => x.FilmId != 0) ? Films.Where(x => x.FilmId != 0).Average(x => x.Rating) : null;

    /// <summary>
    /// The average community rating across the films that have one.
    /// </summary>
    public double? AverageCommunityRating => Films.Any(x => x.CommunityRating is not null) ? Films.Where(x => x.CommunityRating is not null).Average(x => x.CommunityRating!.Value) : null;

    /// <summary>
    /// Loads the collection from Jellyfin and matches its films to my rated films, then to cached library details.
    /// </summary>
    /// <param name="id">The Jellyfin collection ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        List<JellyfinCollection> collections;
        try
        {
            collections = await jellyfin.GetCollectionsAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        JellyfinCollection? collection = collections.FirstOrDefault(x => x.Id == id);
        if (collection is null)
        {
            return NotFound();
        }
        Name = collection.Name;
        List<int> tmdbIds = [.. collection.Movies.Select(x => x.TMDbId).OfType<int>()];
        List<string> imdbIds = [.. collection.Movies.Select(x => x.IMDbId).OfType<string>()];
        List<Film> rated = await db.Films.AsNoTracking().Where(x => (x.TMDbId != null && tmdbIds.Contains(x.TMDbId.Value)) || (x.IMDbId != null && imdbIds.Contains(x.IMDbId))).ToListAsync(cancellationToken);
        Dictionary<int, LibraryFilm> library = await db.LibraryFilms.AsNoTracking().Where(x => tmdbIds.Contains(x.TMDbId)).ToDictionaryAsync(x => x.TMDbId, cancellationToken);
        List<string> libraryImdbIds = [.. library.Values.Select(x => x.IMDbId).OfType<string>()];
        Dictionary<string, OMDbCacheEntry> ratings = await db.OMDbCache.AsNoTracking().Where(x => libraryImdbIds.Contains(x.IMDbId)).ToDictionaryAsync(x => x.IMDbId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        List<Film> films = [];
        foreach (JellyfinItem movie in collection.Movies)
        {
            Film? film = rated.FirstOrDefault(x => (movie.IMDbId is not null && string.Equals(x.IMDbId, movie.IMDbId, StringComparison.OrdinalIgnoreCase)) || (movie.TMDbId is not null && x.TMDbId == movie.TMDbId));
            if (film is null && movie.TMDbId is int tmdbId && library.TryGetValue(tmdbId, out LibraryFilm? libraryFilm))
            {
                film = libraryFilm.ToFilm(libraryFilm.IMDbId is string imdbId ? ratings.GetValueOrDefault(imdbId) : null);
            }
            films.Add(film ?? new Film { Title = movie.Name, Year = movie.ProductionYear, TMDbId = movie.TMDbId, IMDbId = movie.IMDbId });
        }
        Films = [.. films.DistinctBy(x => x.FilmId != 0 ? $"film-{x.FilmId}" : $"tmdb-{x.TMDbId}-{x.Title}").Where(x => x.FilmId != 0 || User.Identity?.IsAuthenticated == true).OrderBy(x => x.Year ?? int.MaxValue).ThenBy(x => x.Title)];
        return Films.Count == 0 ? NotFound() : Page();
    }
}