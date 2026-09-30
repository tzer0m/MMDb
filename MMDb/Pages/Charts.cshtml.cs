using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Helpers;
using MMDb.Models;

namespace MMDb.Pages;

/// <summary>
/// Shows the rating charts on their own page, for phones where they are hidden on the home page.
/// </summary>
/// <param name="db">The database context.</param>
public class ChartsModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// How many films I rated 1 to 10, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] MyRatingCounts { get; set; } = [];

    /// <summary>
    /// How many films have each community rating, rounded to the nearest whole number, indexed from 0 for a rating of 1.
    /// </summary>
    public int[] CommunityRatingCounts { get; set; } = [];

    /// <summary>
    /// Loads every film and counts the ratings.
    /// </summary>
    public async Task OnGetAsync()
    {
        List<Film> films = await db.Films.AsNoTracking().ToListAsync();
        MyRatingCounts = RatingDistribution.ForMyRatings(films);
        CommunityRatingCounts = RatingDistribution.ForCommunityRatings(films);
    }
}