using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Pages.Films;

/// <summary>
/// Shows the full details of a single film, its director and top cast, and my other films they appear in.
/// </summary>
/// <param name="db">The database context.</param>
public class DetailsModel(MMDbContext db) : PageModel
{
    /// <summary>
    /// The film being displayed.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The film's director, if known.
    /// </summary>
    public Person? Director { get; set; }

    /// <summary>
    /// The top five billed cast members.
    /// </summary>
    public List<Person> Cast { get; set; } = [];

    /// <summary>
    /// The character each cast member played, keyed by person ID.
    /// </summary>
    public Dictionary<int, string> Characters { get; set; } = [];

    /// <summary>
    /// My other rated films for each person, keyed by person ID, highest rated first.
    /// </summary>
    public Dictionary<int, List<Film>> OtherFilms { get; set; } = [];

    /// <summary>
    /// Loads the film, its people and their other rated films.
    /// </summary>
    /// <param name="id">The ID of the film to display.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Film? film = await db.Films.AsNoTracking().Include(x => x.Credits).ThenInclude(x => x.Person).FirstOrDefaultAsync(x => x.FilmId == id, cancellationToken);
        if (film is null)
        {
            return NotFound();
        }
        Film = film;
        Director = film.Credits.Where(x => x.Role == CreditRole.Director).OrderBy(x => x.Order).Select(x => x.Person).FirstOrDefault();
        Cast = [.. film.Credits.Where(x => x.Role == CreditRole.Cast).OrderBy(x => x.Order).Take(5).Select(x => x.Person)];
        Characters = film.Credits.Where(x => x.Role == CreditRole.Cast && x.Character != null).GroupBy(x => x.PersonId).ToDictionary(x => x.Key, x => x.First().Character!);
        List<int> personIds = [.. Cast.Select(x => x.PersonId)];
        if (Director is not null)
        {
            personIds.Add(Director.PersonId);
        }
        List<FilmCredit> otherCredits = await db.FilmCredits.AsNoTracking().Include(x => x.Film).Where(x => personIds.Contains(x.PersonId) && x.FilmId != id).ToListAsync(cancellationToken);
        OtherFilms = personIds.Distinct().ToDictionary(personId => personId, personId => otherCredits.Where(x => x.PersonId == personId).Select(x => x.Film).DistinctBy(x => x.FilmId).OrderByDescending(x => x.Rating).ThenBy(x => x.Title).ToList());
        return Page();
    }
}