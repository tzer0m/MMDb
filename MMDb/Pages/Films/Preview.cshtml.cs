using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Services;

namespace MMDb.Pages.Films;

/// <summary>
/// Previews a film from TMDb that is not in MMDb yet, with a form to rate and add it when signed in.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="filmData">The film data service.</param>
/// <param name="filmPeople">The film people service.</param>
/// <param name="jellyfin">The Jellyfin client.</param>
/// <param name="logger">The logger.</param>
public partial class PreviewModel(MMDbContext db, FilmDataService filmData, FilmPeopleService filmPeople, JellyfinClient jellyfin, ILogger<PreviewModel> logger) : PageModel
{
    /// <summary>
    /// The TMDb ID of the film being previewed.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int TMDbId { get; set; }

    /// <summary>
    /// The search that led here, for the Back to Search button.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <summary>
    /// The film built from TMDb and OMDb, not yet saved.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The film's director and top cast, with my films they appear in.
    /// </summary>
    public FilmPeople People { get; set; } = new();

    /// <summary>
    /// The submitted rating form.
    /// </summary>
    [BindProperty]
    public FilmInput Input { get; set; } = new();

    /// <summary>
    /// Shows the preview, or my film page if I have already rated it.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        int? existingId = await FindExistingAsync(cancellationToken);
        if (existingId is int filmId)
        {
            return RedirectToPage("/Films/Details", new { id = filmId });
        }
        if (!await LoadPreviewAsync(cancellationToken))
        {
            return NotFound();
        }
        Input.WatchedOn = DateOnly.FromDateTime(DateTime.Today);
        return Page();
    }

    /// <summary>
    /// Saves the film with my rating, pushes the rating to Jellyfin, then redirects to the new film page.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }
        int? existingId = await FindExistingAsync(cancellationToken);
        if (existingId is int filmId)
        {
            return RedirectToPage("/Films/Details", new { id = filmId });
        }
        if (!ModelState.IsValid)
        {
            return await LoadPreviewAsync(cancellationToken) ? Page() : NotFound();
        }
        Film? film = await BuildFilmAsync(cancellationToken);
        if (film is null)
        {
            return NotFound();
        }
        film.AddedAt = DateTime.UtcNow;
        Input.ApplyTo(film);
        db.Films.Add(film);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Message"] = await PushToJellyfinAsync(film, cancellationToken);
        return RedirectToPage("/Films/Details", new { id = film.FilmId });
    }

    /// <summary>
    /// Builds the preview film and its people, returning false if TMDb has no such film.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<bool> LoadPreviewAsync(CancellationToken cancellationToken)
    {
        Film? film = await BuildFilmAsync(cancellationToken);
        if (film is null)
        {
            return false;
        }
        Film = film;
        People = await filmPeople.LoadAsync(film.Credits, 0, cancellationToken);
        return true;
    }

    /// <summary>
    /// Builds an unsaved film from TMDb details and OMDb ratings, returning null if TMDb has no such film.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<Film?> BuildFilmAsync(CancellationToken cancellationToken)
    {
        Film film = new();
        if (!await filmData.ApplyTMDbAsync(film, TMDbId, cancellationToken))
        {
            return null;
        }
        try
        {
            await filmData.RefreshRatingsAsync(film, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogRatingsFailed(logger, ex, film.Title);
        }
        return film;
    }

    /// <summary>
    /// Returns my film ID if this TMDb film is already in MMDb.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<int?> FindExistingAsync(CancellationToken cancellationToken)
    {
        return await db.Films.Where(x => x.TMDbId == TMDbId).Select(x => (int?)x.FilmId).FirstOrDefaultAsync(cancellationToken);
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
            LogPushFailed(logger, ex, film.Title);
            return $"Saved, but pushing the rating to Jellyfin failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Logs that OMDb ratings could not be fetched for a previewed film.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="title">The film title.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not fetch OMDb ratings for {Title}; the refresh job will retry once it is added.")]
    private static partial void LogRatingsFailed(ILogger logger, Exception exception, string title);

    /// <summary>
    /// Logs that a rating could not be pushed to Jellyfin.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="title">The film title.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not push the rating for {Title} to Jellyfin.")]
    private static partial void LogPushFailed(ILogger logger, Exception exception, string title);
}