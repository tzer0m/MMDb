using Microsoft.AspNetCore.Mvc;
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
    /// Text to filter film titles by.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    /// <summary>
    /// The column to sort by: rating, title, year or watched.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "rating";

    /// <summary>
    /// Whether to sort in descending order.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Desc { get; set; } = true;

    /// <summary>
    /// The films to display.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// The average rating across the displayed films.
    /// </summary>
    public double? AverageRating => Films.Count == 0 ? null : Films.Average(x => x.Rating);

    /// <summary>
    /// Loads the films, applying the search filter and sort order.
    /// </summary>
    public async Task OnGetAsync()
    {
        IQueryable<Film> query = db.Films.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            string pattern = $"%{Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern));
        }
        query = Sort switch
        {
            "title" => Desc ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
            "year" => Desc ? query.OrderBy(x => x.Year == null).ThenByDescending(x => x.Year).ThenBy(x => x.Title) : query.OrderBy(x => x.Year == null).ThenBy(x => x.Year).ThenBy(x => x.Title),
            "watched" => Desc ? query.OrderBy(x => x.WatchedOn == null).ThenByDescending(x => x.WatchedOn).ThenBy(x => x.Title) : query.OrderBy(x => x.WatchedOn == null).ThenBy(x => x.WatchedOn).ThenBy(x => x.Title),
            _ => Desc ? query.OrderByDescending(x => x.Rating).ThenBy(x => x.Title) : query.OrderBy(x => x.Rating).ThenBy(x => x.Title)
        };
        Films = await query.ToListAsync();
    }

    /// <summary>
    /// Builds the route values for a sortable column header, toggling direction if it is the current sort.
    /// </summary>
    /// <param name="column">The column to sort by.</param>
    public Dictionary<string, string> SortRoute(string column)
    {
        bool desc = Sort == column ? !Desc : column != "title";
        Dictionary<string, string> route = new() { ["sort"] = column, ["desc"] = desc.ToString().ToLowerInvariant() };
        if (!string.IsNullOrWhiteSpace(Search))
        {
            route["search"] = Search;
        }
        return route;
    }
}