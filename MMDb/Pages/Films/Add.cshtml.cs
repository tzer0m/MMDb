using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Models.TMDb;
using MMDb.Services;

namespace MMDb.Pages.Films;

/// <summary>
/// Adds a film by searching TMDb, picking a result and rating it.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="filmData">The film data service.</param>
/// <param name="jellyfin">The Jellyfin client.</param>
/// <param name="logger">The logger.</param>
public class AddModel(MMDbContext db, TMDbClient tmdb, FilmDataService filmData, JellyfinClient jellyfin, ILogger<AddModel> logger) : PageModel
{
    /// <summary>
    /// The title to search TMDb for.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <summary>
    /// The TMDb ID of the selected film.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int? TMDbId { get; set; }

    /// <summary>
    /// The TMDb search results.
    /// </summary>
    public List<TMDbSearchResult> Results { get; set; } = [];

    /// <summary>
    /// Films already in MMDb, keyed by TMDb ID, mapped to their film ID.
    /// </summary>
    public Dictionary<int, int> ExistingFilms { get; set; } = [];

    /// <summary>
    /// The director names for each search result, keyed by TMDb ID.
    /// </summary>
    public Dictionary<int, string> Directors { get; set; } = [];

    /// <summary>
    /// The selected film's details from TMDb.
    /// </summary>
    public TMDbMovie? Selected { get; set; }

    /// <summary>
    /// The submitted rating form.
    /// </summary>
    [BindProperty]
    public FilmInput Input { get; set; } = new();

    /// <summary>
    /// Shows TMDb search results, or the rating form for a selected film, returning home if neither was asked for.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (TMDbId is int tmdbId)
        {
            int? existingId = await FindExistingAsync(tmdbId, cancellationToken);
            if (existingId is int filmId)
            {
                return RedirectToPage("/Films/Edit", new { id = filmId });
            }
            Selected = await tmdb.GetMovieAsync(tmdbId, cancellationToken);
            if (Selected is null)
            {
                return NotFound();
            }
            Input.WatchedOn = DateOnly.FromDateTime(DateTime.Today);
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Query))
        {
            return RedirectToPage("/Index");
        }
        Results = await tmdb.SearchAsync(Query.Trim(), cancellationToken);
        List<int> ids = [.. Results.Select(x => x.Id)];
        TMDbMovie?[] details = await Task.WhenAll(ids.Select(x => TryGetMovieAsync(x, cancellationToken)));
        Directors = details.OfType<TMDbMovie>().ToDictionary(x => x.Id, x => string.Join(", ", (x.Credits?.Crew ?? []).Where(c => c.Job == "Director").Select(c => c.Name).Distinct()));
        ExistingFilms = await db.Films.Where(x => x.TMDbId != null && ids.Contains(x.TMDbId.Value)).ToDictionaryAsync(x => x.TMDbId!.Value, x => x.FilmId, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Saves the selected film with its details, ratings and my rating, then pushes the rating to Jellyfin.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (TMDbId is not int tmdbId)
        {
            return RedirectToPage();
        }
        int? existingId = await FindExistingAsync(tmdbId, cancellationToken);
        if (existingId is int filmId)
        {
            return RedirectToPage("/Films/Edit", new { id = filmId });
        }
        if (!ModelState.IsValid)
        {
            Selected = await tmdb.GetMovieAsync(tmdbId, cancellationToken);
            return Selected is null ? NotFound() : Page();
        }
        Film film = new() { AddedAt = DateTime.UtcNow };
        Input.ApplyTo(film);
        if (!await filmData.ApplyTMDbAsync(film, tmdbId, cancellationToken))
        {
            return NotFound();
        }
        try
        {
            await filmData.RefreshRatingsAsync(film, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not fetch OMDb ratings for {Title}; the refresh job will retry.", film.Title);
        }
        db.Films.Add(film);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Message"] = await PushToJellyfinAsync(film, cancellationToken);
        return RedirectToPage("/Films/Details", new { id = film.FilmId });
    }

    /// <summary>
    /// Gets a movie's details from TMDb, returning null instead of throwing if the request fails.
    /// </summary>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<TMDbMovie?> TryGetMovieAsync(int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            return await tmdb.GetMovieAsync(tmdbId, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not load TMDb details for {TMDbId}.", tmdbId);
            return null;
        }
    }

    /// <summary>
    /// Returns the film ID if a film with this TMDb ID is already in MMDb.
    /// </summary>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<int?> FindExistingAsync(int tmdbId, CancellationToken cancellationToken)
    {
        return await db.Films.Where(x => x.TMDbId == tmdbId).Select(x => (int?)x.FilmId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Pushes the rating to Jellyfin and returns a message describing the outcome.
    /// </summary>
    /// <param name="film">The film whose rating to push.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string?> PushToJellyfinAsync(Film film, CancellationToken cancellationToken)
    {
        try
        {
            return await jellyfin.PushRatingAsync(film, cancellationToken) ? null : "Saved. This film isn't in Jellyfin, so no rating was pushed.";
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not push the rating for {Title} to Jellyfin.", film.Title);
            return $"Saved, but pushing the rating to Jellyfin failed: {ex.Message}";
        }
    }
}