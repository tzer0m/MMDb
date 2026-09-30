using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Services;

namespace MMDb.Pages.Films;

/// <summary>
/// Edits a film's rating and watched date, refreshes its data, or deletes it.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="filmData">The film data service.</param>
/// <param name="jellyfin">The Jellyfin client.</param>
/// <param name="logger">The logger.</param>
public partial class EditModel(MMDbContext db, FilmDataService filmData, JellyfinClient jellyfin, ILogger<EditModel> logger) : PageModel
{
    /// <summary>
    /// The ID of the film being edited.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    /// <summary>
    /// The film being edited.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The submitted rating form.
    /// </summary>
    [BindProperty]
    public FilmInput Input { get; set; } = new();

    /// <summary>
    /// Loads the film into the form.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Film? film = await db.Films.AsNoTracking().FirstOrDefaultAsync(x => x.FilmId == Id, cancellationToken);
        if (film is null)
        {
            return NotFound();
        }
        Film = film;
        Input = FilmInput.FromFilm(film);
        return Page();
    }

    /// <summary>
    /// Saves the rating and watched date, pushes the rating to Jellyfin, then redirects to the film's page.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Film? film = await db.Films.FirstOrDefaultAsync(x => x.FilmId == Id, cancellationToken);
        if (film is null)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            Film = film;
            return Page();
        }
        Input.ApplyTo(film);
        await db.SaveChangesAsync(cancellationToken);
        TempData["Message"] = await PushToJellyfinAsync(film, cancellationToken);
        return RedirectToPage("/Films/Details", new { id = film.FilmId });
    }

    /// <summary>
    /// Re-fetches the film's details from TMDb and ratings from OMDb, then redirects to the film's page.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostRefreshAsync(CancellationToken cancellationToken)
    {
        Film? film = await db.Films.FirstOrDefaultAsync(x => x.FilmId == Id, cancellationToken);
        if (film is null)
        {
            return NotFound();
        }
        try
        {
            if (film.TMDbId is int tmdbId && !await filmData.ApplyTMDbAsync(film, tmdbId, cancellationToken))
            {
                TempData["Message"] = $"TMDb no longer has a film with ID {tmdbId}.";
            }
            await filmData.RefreshRatingsAsync(film, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            TempData["Message"] ??= "Film data refreshed.";
        }
        catch (HttpRequestException ex)
        {
            LogRefreshFailed(logger, ex, film.Title);
            TempData["Message"] = $"Refreshing failed: {ex.Message}";
        }
        return RedirectToPage("/Films/Details", new { id = film.FilmId });
    }

    /// <summary>
    /// Deletes the film, leaving its rating in Jellyfin, then redirects to the film list.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken cancellationToken)
    {
        await db.Films.Where(x => x.FilmId == Id).ExecuteDeleteAsync(cancellationToken);
        return RedirectToPage("/Index");
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
    /// Logs that a film's data could not be refreshed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="title">The film title.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not refresh data for {Title}.")]
    private static partial void LogRefreshFailed(ILogger logger, Exception exception, string title);

    /// <summary>
    /// Logs that a rating could not be pushed to Jellyfin.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="title">The film title.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not push the rating for {Title} to Jellyfin.")]
    private static partial void LogPushFailed(ILogger logger, Exception exception, string title);
}