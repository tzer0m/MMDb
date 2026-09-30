using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Pages.People;

/// <summary>
/// Shows a director or actor and every film of theirs I have rated.
/// </summary>
/// <param name="db">The database context.</param>
public class DetailsModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// The person being displayed.
    /// </summary>
    public Person Person { get; set; } = null!;

    /// <summary>
    /// Their films I have rated, highest rated first.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// Their role on each film, keyed by film ID, e.g. Director or Director, Cast.
    /// </summary>
    public Dictionary<int, string> Roles { get; set; } = [];

    /// <summary>
    /// My average rating across their films.
    /// </summary>
    public double? AverageMyRating => Films.Count == 0 ? null : Films.Average(x => x.Rating);

    /// <summary>
    /// The average community rating across their films that have one.
    /// </summary>
    public double? AverageCommunityRating => Films.Any(x => x.CommunityRating is not null) ? Films.Where(x => x.CommunityRating is not null).Average(x => x.CommunityRating!.Value) : null;

    /// <summary>
    /// Loads the person and their rated films.
    /// </summary>
    /// <param name="id">The TMDb person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Person? person = await db.People.AsNoTracking().FirstOrDefaultAsync(x => x.PersonId == id, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }
        Person = person;
        List<FilmCredit> credits = await db.FilmCredits.AsNoTracking().Include(x => x.Film).Where(x => x.PersonId == id).ToListAsync(cancellationToken);
        Films = [.. credits.Select(x => x.Film).DistinctBy(x => x.FilmId).OrderByDescending(x => x.Rating).ThenBy(x => x.Title)];
        Roles = credits.GroupBy(x => x.FilmId).ToDictionary(x => x.Key, x => string.Join(", ", x.Select(c => c.Role).Distinct().Order()));
        return Page();
    }
}