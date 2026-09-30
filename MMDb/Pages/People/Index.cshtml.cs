using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Pages.People;

/// <summary>
/// Lists every director and actor in my rated films, with totals across those films.
/// </summary>
/// <param name="db">The database context.</param>
public class IndexModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// Every person with at least one credit, most films first.
    /// </summary>
    public List<TalentSummary> Talent { get; set; } = [];

    /// <summary>
    /// Loads every credit and totals them per person; searching and sorting happen in the browser.
    /// </summary>
    public async Task OnGetAsync()
    {
        List<FilmCredit> credits = await db.FilmCredits.AsNoTracking().Include(x => x.Person).Include(x => x.Film).ToListAsync();
        Talent = [.. credits.GroupBy(x => x.PersonId).Select(ToSummary).OrderByDescending(x => x.FilmCount).ThenBy(x => x.Name)];
    }

    /// <summary>
    /// Totals one person's credits into a summary row.
    /// </summary>
    /// <param name="credits">The person's credits.</param>
    private static TalentSummary ToSummary(IGrouping<int, FilmCredit> credits)
    {
        List<Film> films = [.. credits.Select(x => x.Film).DistinctBy(x => x.FilmId)];
        List<double> communityRatings = [.. films.Where(x => x.CommunityRating is not null).Select(x => x.CommunityRating!.Value)];
        Person person = credits.First().Person;
        return new TalentSummary { PersonId = person.PersonId, Name = person.Name, ProfilePath = person.ProfilePath, FilmCount = films.Count, Roles = string.Join("/", credits.Select(x => x.Role).Distinct().Order().Select(x => x == CreditRole.Director ? "D" : "A")), AverageMyRating = Math.Round(films.Average(x => x.Rating), 1), AverageCommunityRating = communityRatings.Count == 0 ? null : Math.Round(communityRatings.Average(), 1) };
    }
}