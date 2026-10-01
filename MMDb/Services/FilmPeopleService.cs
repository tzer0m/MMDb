using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Models.TMDb;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Builds the director and cast section shown on film pages and previews, and serves people's cached TMDb credits.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="cache">The memory cache for TMDb credits.</param>
/// <param name="options">The people options.</param>
/// <param name="logger">The logger.</param>
public partial class FilmPeopleService(MMDbContext db, TMDbClient tmdb, IMemoryCache cache, IOptions<PeopleOptions> options, ILogger<FilmPeopleService> logger)
{
    /// <summary>
    /// Picks the director and top-billed cast from a film's credits and loads my other rated films for each, including ones where they are billed below the stored cast list.
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
            Cast = [.. creditList.Where(x => x.Role == CreditRole.Cast).OrderBy(x => x.Order).Take(options.Value.CastCount).Select(x => x.Person)],
            Characters = creditList.Where(x => x.Role == CreditRole.Cast && x.Character != null).GroupBy(x => x.PersonId).ToDictionary(x => x.Key, x => x.First().Character!)
        };
        List<int> personIds = [.. people.Cast.Select(x => x.PersonId)];
        if (people.Director is not null)
        {
            personIds.Add(people.Director.PersonId);
        }
        personIds = [.. personIds.Distinct()];
        List<FilmCredit> otherCredits = await db.FilmCredits.AsNoTracking().Include(x => x.Film).Where(x => personIds.Contains(x.PersonId) && x.FilmId != excludeFilmId).ToListAsync(cancellationToken);
        TMDbPersonCredits?[] personCredits = await Task.WhenAll(personIds.Select(x => GetPersonCreditsAsync(x, cancellationToken)));
        Dictionary<int, HashSet<int>> tmdbIdsByPerson = personIds.Zip(personCredits).ToDictionary(x => x.First, x => x.Second is null ? [] : x.Second.Cast.Select(c => c.Id).Concat(x.Second.Crew.Where(c => c.Job == "Director").Select(c => c.Id)).ToHashSet());
        List<int> tmdbIds = [.. tmdbIdsByPerson.Values.SelectMany(x => x).Distinct()];
        List<Film> creditedFilms = await db.Films.AsNoTracking().Where(x => x.TMDbId != null && tmdbIds.Contains(x.TMDbId.Value) && x.FilmId != excludeFilmId).ToListAsync(cancellationToken);
        people.OtherFilms = personIds.ToDictionary(personId => personId, personId => otherCredits.Where(x => x.PersonId == personId).Select(x => x.Film).Concat(creditedFilms.Where(x => tmdbIdsByPerson[personId].Contains(x.TMDbId!.Value))).DistinctBy(x => x.FilmId).OrderByDescending(x => x.Rating).ThenBy(x => x.Title).ToList());
        return people;
    }

    /// <summary>
    /// Gets a person's TMDb film credits from the memory cache, fetching them if needed, or null if TMDb fails.
    /// </summary>
    /// <param name="personId">The TMDb person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TMDbPersonCredits?> GetPersonCreditsAsync(int personId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await cache.GetOrCreateAsync($"person-credits-{personId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = options.Value.CreditsCacheDuration;
                return await tmdb.GetPersonMovieCreditsAsync(personId, cancellationToken);
            });
        }
        catch (HttpRequestException ex)
        {
            LogCreditsFailed(logger, ex, personId);
            return null;
        }
    }

    /// <summary>
    /// Logs that a person's film credits could not be fetched from TMDb.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="personId">The TMDb person ID.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not fetch TMDb film credits for person {PersonId}.")]
    private static partial void LogCreditsFailed(ILogger logger, Exception exception, int personId);
}