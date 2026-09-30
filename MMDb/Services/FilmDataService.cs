using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Models.OMDb;
using MMDb.Models.TMDb;

namespace MMDb.Services;

/// <summary>
/// Fills in film details and credits from TMDb and external ratings from OMDb.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="omdb">The OMDb client.</param>
public class FilmDataService(MMDbContext db, TMDbClient tmdb, OMDbClient omdb)
{
    /// <summary>
    /// Copies a movie's details and credits from TMDb onto a film, returning false if TMDb has no such movie.
    /// </summary>
    /// <param name="film">The film to update.</param>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<bool> ApplyTMDbAsync(Film film, int tmdbId, CancellationToken cancellationToken = default)
    {
        TMDbMovie? movie = await tmdb.GetMovieAsync(tmdbId, cancellationToken);
        if (movie is null)
        {
            return false;
        }
        List<TMDbCrewMember> crew = movie.Credits?.Crew ?? [];
        List<TMDbCastMember> cast = movie.Credits?.Cast ?? [];
        List<string> directors = [.. crew.Where(x => x.Job == "Director").Select(x => x.Name).Distinct()];
        film.TMDbId = movie.Id;
        film.IMDbId = string.IsNullOrWhiteSpace(movie.ImdbId) ? film.IMDbId : movie.ImdbId;
        film.Title = movie.Title;
        film.Year = movie.Year;
        film.Director = directors.Count == 0 ? null : string.Join(", ", directors);
        film.RuntimeMinutes = movie.Runtime is > 0 ? movie.Runtime : null;
        film.Overview = string.IsNullOrWhiteSpace(movie.Overview) ? null : movie.Overview;
        film.Tagline = string.IsNullOrWhiteSpace(movie.Tagline) ? null : movie.Tagline;
        film.Genres = [.. movie.Genres.Select(x => x.Name)];
        film.Cast = [.. cast.OrderBy(x => x.Order).Take(10).Select(x => x.Name)];
        film.PosterPath = movie.PosterPath;
        film.BackdropPath = movie.BackdropPath;
        film.TMDbRating = movie.VoteCount > 0 ? Math.Round(movie.VoteAverage, 1) : null;
        film.UpdatedAt = DateTime.UtcNow;
        await ApplyCreditsAsync(film, crew, cast, cancellationToken);
        return true;
    }

    /// <summary>
    /// Refreshes a film's IMDb, Rotten Tomatoes and Metacritic ratings from OMDb.
    /// </summary>
    /// <param name="film">The film to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task RefreshRatingsAsync(Film film, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(film.IMDbId))
        {
            OMDbMovie? movie = await omdb.GetByImdbIdAsync(film.IMDbId, cancellationToken);
            if (movie is not null)
            {
                film.IMDbRating = movie.ImdbRatingValue;
                film.IMDbVotes = movie.ImdbVotesValue;
                film.RottenTomatoes = movie.RottenTomatoesValue;
                film.Metacritic = movie.MetacriticValue;
            }
        }
        film.RatingsUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Syncs a film's director and top 10 cast credits, creating or updating the people involved.
    /// </summary>
    /// <param name="film">The film to update.</param>
    /// <param name="crew">The crew from TMDb.</param>
    /// <param name="cast">The cast from TMDb.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task ApplyCreditsAsync(Film film, List<TMDbCrewMember> crew, List<TMDbCastMember> cast, CancellationToken cancellationToken)
    {
        List<(int PersonId, string Name, string? ProfilePath, CreditRole Role, int Order)> wanted = [.. crew.Where(x => x.Job == "Director").DistinctBy(x => x.Id).Select((x, index) => (x.Id, x.Name, x.ProfilePath, CreditRole.Director, index))];
        wanted.AddRange(cast.OrderBy(x => x.Order).DistinctBy(x => x.Id).Take(10).Select(x => (x.Id, x.Name, x.ProfilePath, CreditRole.Cast, x.Order)));
        List<int> personIds = [.. wanted.Select(x => x.PersonId).Distinct()];
        Dictionary<int, Person> people = db.People.Local.Where(x => personIds.Contains(x.PersonId)).ToDictionary(x => x.PersonId);
        List<int> untracked = [.. personIds.Where(x => !people.ContainsKey(x))];
        foreach (Person person in await db.People.Where(x => untracked.Contains(x.PersonId)).ToListAsync(cancellationToken))
        {
            people[person.PersonId] = person;
        }
        foreach ((int PersonId, string Name, string? ProfilePath, CreditRole Role, int Order) credit in wanted)
        {
            if (!people.TryGetValue(credit.PersonId, out Person? person))
            {
                person = new Person { PersonId = credit.PersonId };
                db.People.Add(person);
                people[credit.PersonId] = person;
            }
            person.Name = credit.Name;
            person.ProfilePath = credit.ProfilePath;
        }
        if (film.FilmId != 0)
        {
            await db.Entry(film).Collection(x => x.Credits).LoadAsync(cancellationToken);
        }
        film.Credits.RemoveAll(x => !wanted.Any(w => w.PersonId == x.PersonId && w.Role == x.Role));
        foreach ((int PersonId, string Name, string? ProfilePath, CreditRole Role, int Order) credit in wanted)
        {
            FilmCredit? existing = film.Credits.FirstOrDefault(x => x.PersonId == credit.PersonId && x.Role == credit.Role);
            if (existing is null)
            {
                film.Credits.Add(new FilmCredit { PersonId = credit.PersonId, Person = people[credit.PersonId], Role = credit.Role, Order = credit.Order });
            }
            else
            {
                existing.Order = credit.Order;
            }
        }
    }
}