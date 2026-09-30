using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Services;

/// <summary>
/// Builds the director and cast section shown on film pages and previews.
/// </summary>
/// <param name="db">The database context.</param>
public class FilmPeopleService(MMDbContext db)
{
    /// <summary>
    /// Picks the director and top five cast from a film's credits and loads my other rated films for each.
    /// </summary>
    /// <param name="credits">The film's credits, with people loaded.</param>
    /// <param name="excludeFilmId">The film to leave out of each person's other films, or 0 for a film not yet in MMDb.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<FilmPeople> LoadAsync(IEnumerable<FilmCredit> credits, int excludeFilmId, CancellationToken cancellationToken = default)
    {
        List<FilmCredit> creditList = [.. credits];
        FilmPeople people = new()
        {
            Director = creditList.Where(x => x.Role == CreditRole.Director).OrderBy(x => x.Order).Select(x => x.Person).FirstOrDefault(),
            Cast = [.. creditList.Where(x => x.Role == CreditRole.Cast).OrderBy(x => x.Order).Take(5).Select(x => x.Person)],
            Characters = creditList.Where(x => x.Role == CreditRole.Cast && x.Character != null).GroupBy(x => x.PersonId).ToDictionary(x => x.Key, x => x.First().Character!)
        };
        List<int> personIds = [.. people.Cast.Select(x => x.PersonId)];
        if (people.Director is not null)
        {
            personIds.Add(people.Director.PersonId);
        }
        List<FilmCredit> otherCredits = await db.FilmCredits.AsNoTracking().Include(x => x.Film).Where(x => personIds.Contains(x.PersonId) && x.FilmId != excludeFilmId).ToListAsync(cancellationToken);
        people.OtherFilms = personIds.Distinct().ToDictionary(personId => personId, personId => otherCredits.Where(x => x.PersonId == personId).Select(x => x.Film).DistinctBy(x => x.FilmId).OrderByDescending(x => x.Rating).ThenBy(x => x.Title).ToList());
        return people;
    }
}