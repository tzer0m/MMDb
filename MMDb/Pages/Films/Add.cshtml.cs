using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Pages.Films;

/// <summary>
/// Adds a new film.
/// </summary>
/// <param name="db">The database context.</param>
public class AddModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// The submitted form values.
    /// </summary>
    [BindProperty]
    public FilmInput Input { get; set; } = new();

    /// <summary>
    /// Shows the empty add form.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Validates and saves the new film, then redirects to its page.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        Film film = new() { AddedAt = DateTime.UtcNow };
        Input.ApplyTo(film);
        db.Films.Add(film);
        await db.SaveChangesAsync();
        return RedirectToPage("/Films/Details", new { id = film.FilmId });
    }
}