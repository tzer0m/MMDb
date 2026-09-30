using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Helpers;
using MMDb.Models;
using MMDb.Services;

namespace MMDb.Pages.Films;

/// <summary>
/// Shows the full details of a single film, its director and top cast, and my other films they appear in.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="filmPeople">The film people service.</param>
/// <param name="jellyfin">The Jellyfin client.</param>
public class DetailsModel(MMDbContext db, FilmPeopleService filmPeople, JellyfinClient jellyfin) : PageModel
{
    /// <summary>
    /// The film being displayed.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The film's director and top cast, with my other films they appear in.
    /// </summary>
    public FilmPeople People { get; set; } = new();

    /// <summary>
    /// Loads the film, its people and their other rated films.
    /// </summary>
    /// <param name="id">The ID of the film to display.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Film? film = await db.Films.AsNoTracking().Include(x => x.Credits).ThenInclude(x => x.Person).FirstOrDefaultAsync(x => x.FilmId == id, cancellationToken);
        if (film is null)
        {
            return NotFound();
        }
        Film = film;
        People = await filmPeople.LoadAsync(film.Credits, film.FilmId, cancellationToken);
        ViewData["ShowSwagBagger"] = await SwagBaggerButton.ShouldShowAsync(User, jellyfin, film, cancellationToken);
        return Page();
    }
}