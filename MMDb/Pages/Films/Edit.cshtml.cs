using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Pages.Films;

/// <summary>
/// Edits or deletes an existing film.
/// </summary>
/// <param name="db">The database context.</param>
public class EditModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// The ID of the film being edited.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    /// <summary>
    /// The submitted form values.
    /// </summary>
    [BindProperty]
    public FilmInput Input { get; set; } = new();

    /// <summary>
    /// Loads the film into the form.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        Film? film = await db.Films.AsNoTracking().FirstOrDefaultAsync(x => x.FilmId == Id);
        if (film is null)
        {
            return NotFound();
        }
        Input = FilmInput.FromFilm(film);
        return Page();
    }

    /// <summary>
    /// Validates and saves the changes, then redirects to the film's page.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        Film? film = await db.Films.FirstOrDefaultAsync(x => x.FilmId == Id);
        if (film is null)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }
        Input.ApplyTo(film);
        await db.SaveChangesAsync();
        return RedirectToPage("/Films/Details", new { id = film.FilmId });
    }

    /// <summary>
    /// Deletes the film, then redirects to the film list.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await db.Films.Where(x => x.FilmId == Id).ExecuteDeleteAsync();
        return RedirectToPage("/Index");
    }
}