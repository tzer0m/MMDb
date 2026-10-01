using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Helpers;
using MMDb.Models;
using MMDb.Models.TMDb;
using MMDb.Options;
using MMDb.Services;

namespace MMDb.Pages.People;

/// <summary>
/// Shows a director or actor, every film of theirs I have rated, and their top rated films on TMDb.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="filmData">The film data service.</param>
/// <param name="filmPeople">The film people service, for cached TMDb credits.</param>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="omdbCache">The cached OMDb ratings.</param>
/// <param name="jellyfin">The Jellyfin client, for library flags.</param>
/// <param name="options">The people options.</param>
/// <param name="logger">The logger.</param>
public partial class DetailsModel(MMDbContext db, FilmDataService filmData, FilmPeopleService filmPeople, TMDbClient tmdb, OMDbCacheService omdbCache, JellyfinClient jellyfin, IOptions<PeopleOptions> options, ILogger<DetailsModel> logger) : PageModel
{
    /// <summary>
    /// The person being displayed.
    /// </summary>
    public Person Person { get; set; } = null!;

    /// <summary>
    /// Their films I have rated, oldest first, from both the stored cast lists and their full TMDb credits.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// Their role on each film, keyed by film ID: Director, the character they played, or both.
    /// </summary>
    public Dictionary<int, string> Roles { get; set; } = [];

    /// <summary>
    /// Their top rated films on TMDb, including ones I have seen.
    /// </summary>
    public List<TopRatedFilm> TopRated { get; set; } = [];

    /// <summary>
    /// My average rating across their films.
    /// </summary>
    public double? AverageMyRating => Films.Count == 0 ? null : Films.Average(x => x.Rating);

    /// <summary>
    /// The average community rating across their films that have one.
    /// </summary>
    public double? AverageCommunityRating => Films.Any(x => x.CommunityRating is not null) ? Films.Where(x => x.CommunityRating is not null).Average(x => x.CommunityRating!.Value) : null;

    /// <summary>
    /// Their birth and death dates with age, and birthplace, e.g. 3 March 1965 (61) · London, England.
    /// </summary>
    public string BirthLine
    {
        get
        {
            List<string> parts = [];
            if (Person.Birthday is DateOnly birthday)
            {
                parts.Add(Person.Deathday is DateOnly deathday ? $"{birthday:d MMMM yyyy} \u2013 {deathday:d MMMM yyyy} ({Age})" : $"{birthday:d MMMM yyyy} ({Age})");
            }
            if (!string.IsNullOrWhiteSpace(Person.PlaceOfBirth))
            {
                parts.Add(Person.PlaceOfBirth);
            }
            return string.Join(" \u00b7 ", parts);
        }
    }

    /// <summary>
    /// The first two paragraphs of their biography.
    /// </summary>
    public string? BiographySummary => Person.Biography is string biography ? string.Join("\n\n", biography.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(2)) : null;

    /// <summary>
    /// Their age now, or at death if they have died.
    /// </summary>
    public int? Age
    {
        get
        {
            if (Person.Birthday is not DateOnly birthday)
            {
                return null;
            }
            DateOnly end = Person.Deathday ?? DateOnly.FromDateTime(DateTime.Today);
            int age = end.Year - birthday.Year;
            return end < birthday.AddYears(age) ? age - 1 : age;
        }
    }

    /// <summary>
    /// Loads the person, refreshing their details from TMDb if stale, and their rated films.
    /// </summary>
    /// <param name="id">The TMDb person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Person? person = await db.People.FirstOrDefaultAsync(x => x.PersonId == id, cancellationToken);
        if (person is null)
        {
            // Someone from a film I have not rated yet: fetch them from TMDb and store them so their details are cached.
            person = new Person { PersonId = id };
            try
            {
                if (!await filmData.ApplyPersonDetailsAsync(person, cancellationToken))
                {
                    return NotFound();
                }
            }
            catch (HttpRequestException ex)
            {
                LogDetailsFailed(logger, ex, id.ToString());
                return NotFound();
            }
            db.People.Add(person);
            await db.SaveChangesAsync(cancellationToken);
        }
        if (person.DetailsUpdatedAt is null || person.DetailsUpdatedAt < DateTime.UtcNow - options.Value.DetailsMaxAge)
        {
            try
            {
                if (await filmData.ApplyPersonDetailsAsync(person, cancellationToken))
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                LogDetailsFailed(logger, ex, person.Name);
            }
        }
        Person = person;
        List<FilmCredit> credits = await db.FilmCredits.AsNoTracking().Include(x => x.Film).Where(x => x.PersonId == id).ToListAsync(cancellationToken);
        List<Film> films = [.. credits.Select(x => x.Film).DistinctBy(x => x.FilmId)];
        Roles = credits.GroupBy(x => x.FilmId).ToDictionary(x => x.Key, x => string.Join(", ", x.OrderBy(c => c.Role).Select(c => c.Role == CreditRole.Director ? "Director" : c.Character ?? "Cast")));
        TMDbPersonCredits? tmdbCredits = await filmPeople.GetPersonCreditsAsync(id, cancellationToken);
        if (tmdbCredits is not null)
        {
            // Films I have rated where they are billed below the stored cast list, found from their full TMDb credits.
            List<TMDbPersonCredit> all = [.. tmdbCredits.Cast, .. tmdbCredits.Crew.Where(x => x.Job == "Director")];
            List<int> tmdbIds = [.. all.Select(x => x.Id).Distinct()];
            List<int> knownIds = [.. films.Select(x => x.FilmId)];
            List<Film> extra = await db.Films.AsNoTracking().Where(x => x.TMDbId != null && tmdbIds.Contains(x.TMDbId.Value) && !knownIds.Contains(x.FilmId)).ToListAsync(cancellationToken);
            foreach (Film film in extra)
            {
                Roles[film.FilmId] = DescribeRole(all.Where(x => x.Id == film.TMDbId));
            }
            films.AddRange(extra);
        }
        Films = [.. films.OrderBy(x => x.Year ?? int.MaxValue).ThenBy(x => x.Title)];
        TopRated = await LoadTopRatedAsync(tmdbCredits, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Describes a person's role on a film from their TMDb credits: Director, the character they played, or both.
    /// </summary>
    /// <param name="credits">Their TMDb credits for one film.</param>
    private static string DescribeRole(IEnumerable<TMDbPersonCredit> credits)
    {
        return string.Join(", ", credits.OrderBy(x => x.Job == "Director" ? 0 : 1).Select(x => x.Job == "Director" ? "Director" : CharacterName.Clean(x.Character) ?? "Cast").Distinct());
    }

    /// <summary>
    /// Loads a person's top rated films from their cached TMDb credits, marking the ones I have rated and working out community ratings, sorted by community rating.
    /// </summary>
    /// <param name="credits">Their TMDb film credits, or null if they could not be loaded.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<List<TopRatedFilm>> LoadTopRatedAsync(TMDbPersonCredits? credits, CancellationToken cancellationToken)
    {
        if (credits is null)
        {
            return [];
        }
        List<TMDbPersonCredit> all = [.. credits.Cast, .. credits.Crew.Where(x => x.Job == "Director")];
        List<TopRatedFilm> topRated = [.. all.Where(x => x.VoteCount >= options.Value.TopRatedMinVotes).GroupBy(x => x.Id).Select(x => new TopRatedFilm { TMDbId = x.Key, Title = x.First().Title, Year = x.First().Year, PosterPath = x.First().PosterPath, TMDbRating = Math.Round(x.First().VoteAverage, 1), Role = DescribeRole(x) }).OrderByDescending(x => x.TMDbRating).ThenBy(x => x.Title).Take(options.Value.TopRatedCount)];
        List<int> tmdbIds = [.. topRated.Select(x => x.TMDbId)];
        Dictionary<int, Film> seen = await db.Films.AsNoTracking().Where(x => x.TMDbId != null && tmdbIds.Contains(x.TMDbId.Value)).ToDictionaryAsync(x => x.TMDbId!.Value, cancellationToken);
        TMDbMovie?[] details = await Task.WhenAll(topRated.Where(x => !seen.ContainsKey(x.TMDbId)).Select(x => TryGetMovieAsync(x.TMDbId, cancellationToken)));
        Dictionary<int, string> imdbIds = details.OfType<TMDbMovie>().Where(x => !string.IsNullOrWhiteSpace(x.ImdbId)).ToDictionary(x => x.Id, x => x.ImdbId!);
        Dictionary<string, OMDbCacheEntry> ratings = await omdbCache.GetManyAsync(imdbIds.Values, cancellationToken);
        HashSet<int> library = await GetLibraryAsync(cancellationToken);
        foreach (TopRatedFilm film in topRated)
        {
            film.InLibrary = library.Contains(film.TMDbId);
            if (seen.TryGetValue(film.TMDbId, out Film? rated))
            {
                film.FilmId = rated.FilmId;
                SetCommunityRating(film, rated.TMDbRating, rated.IMDbRating, rated.RottenTomatoes, rated.Metacritic);
            }
            else
            {
                OMDbCacheEntry? entry = imdbIds.TryGetValue(film.TMDbId, out string? imdbId) ? ratings.GetValueOrDefault(imdbId) : null;
                SetCommunityRating(film, film.TMDbRating, entry?.IMDbRating, entry?.RottenTomatoes, entry?.Metacritic);
            }
        }
        return [.. topRated.OrderByDescending(x => x.CommunityRating ?? x.TMDbRating).ThenBy(x => x.Title)];
    }

    /// <summary>
    /// Sets a top rated film's community rating and its sources.
    /// </summary>
    /// <param name="film">The film to update.</param>
    /// <param name="tmdbRating">The TMDb rating, out of 10.</param>
    /// <param name="imdbRating">The IMDb rating, out of 10.</param>
    /// <param name="rottenTomatoes">The Rotten Tomatoes score, as a percentage.</param>
    /// <param name="metacritic">The Metacritic score, out of 100.</param>
    private static void SetCommunityRating(TopRatedFilm film, double? tmdbRating, double? imdbRating, int? rottenTomatoes, int? metacritic)
    {
        film.CommunityRating = CommunityRatingCalculator.Calculate(tmdbRating, imdbRating, rottenTomatoes, metacritic);
        film.RatingSources = CommunityRatingCalculator.Describe(tmdbRating, imdbRating, rottenTomatoes, metacritic);
    }

    /// <summary>
    /// Gets the TMDb IDs in my Jellyfin library, or an empty set if not signed in or Jellyfin can't be reached.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<HashSet<int>> GetLibraryAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return [];
        }
        try
        {
            return await jellyfin.GetLibraryTMDbIdsAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogLibraryFailed(logger, ex);
            return [];
        }
    }

    /// <summary>
    /// Logs that the Jellyfin library could not be loaded, so no library flags are shown.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not load the Jellyfin library; showing no library flags.")]
    private static partial void LogLibraryFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Gets a movie's details from TMDb for its IMDb ID, returning null instead of throwing if the request fails.
    /// </summary>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<TMDbMovie?> TryGetMovieAsync(int tmdbId, CancellationToken cancellationToken)
    {
        try
        {
            return await tmdb.GetMovieAsync(tmdbId, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogMovieFailed(logger, ex, tmdbId);
            return null;
        }
    }

    /// <summary>
    /// Logs that a film's TMDb details could not be loaded, so its rating falls back to TMDb alone.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not load TMDb details for {TMDbId}; using the TMDb rating alone.")]
    private static partial void LogMovieFailed(ILogger logger, Exception exception, int tmdbId);

    /// <summary>
    /// Logs that a person's details could not be fetched from TMDb.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="name">The person's name.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not fetch TMDb details for {Name}; showing cached details.")]
    private static partial void LogDetailsFailed(ILogger logger, Exception exception, string name);
}