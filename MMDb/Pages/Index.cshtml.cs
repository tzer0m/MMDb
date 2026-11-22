using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Helpers;
using MMDb.Models;
using MMDb.Options;

namespace MMDb.Pages;

/// <summary>
/// The home page, with rating and watched charts and every rated film in a sortable table.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="chartOptions">The chart options.</param>
public class IndexModel(MMDbContext db, IOptions<ChartOptions> chartOptions) : PageModel
{
    /// <summary>
    /// All rated films, most recently watched first.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// How many films I rated 1 to 10, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] MyRatingCounts { get; set; } = [];

    /// <summary>
    /// How many films have each community rating, rounded to the nearest whole number, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] CommunityRatingCounts { get; set; } = [];

    /// <summary>
    /// How many films were watched in each week, from the configured start date.
    /// </summary>
    public List<WeeklyCount> WatchedCounts { get; set; } = [];

    /// <summary>
    /// Loads every rated film and counts the ratings for the charts; sorting happens in the browser.
    /// </summary>
    public async Task OnGetAsync()
    {
        Films = await db.Films.AsNoTracking().Include(x => x.FilmScore).OrderByDescending(x => x.WatchedOn).ThenBy(x => x.Title).ToListAsync();
        MyRatingCounts = RatingDistribution.ForMyRatings(Films);
        CommunityRatingCounts = RatingDistribution.ForCommunityRatings(Films);
        WatchedCounts = WatchedDistribution.ForWeeks(Films, chartOptions.Value.WatchedStartDate);
    }
}