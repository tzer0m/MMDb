using MMDb.Models;
using MMDb.Models.OMDb;
using MMDb.Models.TMDb;

namespace MMDb.Services;

/// <summary>
/// Fills in film details from TMDb and external ratings from OMDb.
/// </summary>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="omdb">The OMDb client.</param>
public class FilmDataService(TMDbClient tmdb, OMDbClient omdb)
{
    /// <summary>
    /// Copies a movie's details from TMDb onto a film, returning false if TMDb has no such movie.
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
}