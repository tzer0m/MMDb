using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Pages;

/// <summary>
/// The home page, listing all rated films in a table.
/// </summary>
/// <param name="db">The database context.</param>
public class IndexModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// All rated films, most recently watched first.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// Loads every film; searching and sorting happen in the browser.
    /// </summary>
    public async Task OnGetAsync()
    {
        Films = await db.Films.AsNoTracking().OrderByDescending(x => x.WatchedOn).ThenBy(x => x.Title).ToListAsync();
    }
}