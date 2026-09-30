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
    /// How many films I rated 1 to 10, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] MyRatingCounts { get; set; } = new int[10];

    /// <summary>
    /// How many films have each community rating, rounded to the nearest whole number, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] CommunityRatingCounts { get; set; } = new int[10];

    /// <summary>
    /// Loads every film and counts the ratings for the charts; searching and sorting happen in the browser.
    /// </summary>
    public async Task OnGetAsync()
    {
        Films = await db.Films.AsNoTracking().OrderByDescending(x => x.WatchedOn).ThenBy(x => x.Title).ToListAsync();
        foreach (Film film in Films)
        {
            MyRatingCounts[Math.Clamp(film.Rating, 1, 10) - 1]++;
            if (film.CommunityRating is double communityRating)
            {
                CommunityRatingCounts[Math.Clamp((int)Math.Round(communityRating, MidpointRounding.AwayFromZero), 1, 10) - 1]++;
            }
        }
    }
}