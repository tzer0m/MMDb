using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Pages.Films;

/// <summary>
/// Shows the full details of a single film.
/// </summary>
/// <param name="db">The database context.</param>
public class DetailsModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// The film being displayed.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// Loads the film by its ID.
    /// </summary>
    /// <param name="id">The ID of the film to display.</param>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Film? film = await db.Films.AsNoTracking().FirstOrDefaultAsync(x => x.FilmId == id);
        if (film is null)
        {
            return NotFound();
        }
        Film = film;
        return Page();
    }
}