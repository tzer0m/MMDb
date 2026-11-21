using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Helpers;
using MMDb.Models;
using MMDb.Services;

namespace MMDb.Pages.Films;

/// <summary>
/// Lists my rated films, and unrated films in my Jellyfin library when signed in, filtered and a page at a time.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="posters">The poster service.</param>
public class IndexModel(MMDbContext db, PosterService posters) : PageModel
{
    /// <summary>
    /// The filters from the query string, used to fill in the form.
    /// </summary>
    public FilmFilter Filter { get; set; } = new();

    /// <summary>
    /// Shows the page with the form filled in from the query string; the rows are loaded separately.
    /// </summary>
    /// <param name="filter">The filters from the query string.</param>
    public void OnGet([FromQuery] FilmFilter filter)
    {
        Filter = filter;
    }

    /// <summary>
    /// Returns one page of films for the table, filtered and sorted from the DataTables request.
    /// </summary>
    /// <param name="filter">The filters from the form.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetDataAsync([FromQuery] FilmFilter filter, CancellationToken cancellationToken)
    {
        DataTablesRequest request = DataTablesRequest.FromQuery(Request.Query);
        List<Film> films = await db.Films.AsNoTracking().Select(x => new Film { FilmId = x.FilmId, TMDbId = x.TMDbId, IMDbId = x.IMDbId, Title = x.Title, Year = x.Year, Director = x.Director, PosterPath = x.PosterPath, TMDbRating = x.TMDbRating, IMDbRating = x.IMDbRating, RottenTomatoes = x.RottenTomatoes, Metacritic = x.Metacritic, Rating = x.Rating, WatchedOn = x.WatchedOn }).ToListAsync(cancellationToken);
        if (User.Identity?.IsAuthenticated == true && filter.Status != "watched")
        {
            films.AddRange(await LoadUnratedAsync(films, cancellationToken));
        }
        HashSet<int>? castFilmIds = string.IsNullOrWhiteSpace(filter.Cast) ? null : await LoadCastFilmIdsAsync(filter.Cast, cancellationToken);
        List<Film> matches = [.. Sort(request, films.Where(x => filter.Matches(x, castFilmIds)))];
        List<FilmRow> rows = [];
        foreach (Film film in request.PageOf(matches))
        {
            rows.Add(await ToRowAsync(film, cancellationToken));
        }
        return new JsonResult(request.ToPage(rows, films.Count, matches.Count));
    }

    /// <summary>
    /// Loads the unrated films in my Jellyfin library as unsaved films, with any cached ratings applied.
    /// </summary>
    /// <param name="films">The rated films, which are left out.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<List<Film>> LoadUnratedAsync(List<Film> films, CancellationToken cancellationToken)
    {
        HashSet<int> rated = [.. films.Select(x => x.TMDbId).OfType<int>()];
        List<LibraryFilm> libraryFilms = [.. (await db.LibraryFilms.AsNoTracking().ToListAsync(cancellationToken)).Where(x => !rated.Contains(x.TMDbId))];
        List<string> imdbIds = [.. libraryFilms.Select(x => x.IMDbId).OfType<string>()];
        Dictionary<string, OMDbCacheEntry> ratings = await db.OMDbCache.AsNoTracking().Where(x => imdbIds.Contains(x.IMDbId)).ToDictionaryAsync(x => x.IMDbId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        return [.. libraryFilms.Select(x => x.ToFilm(x.IMDbId is string imdbId ? ratings.GetValueOrDefault(imdbId) : null))];
    }

    /// <summary>
    /// Finds the films with someone in the cast whose name contains the search, ignoring case, accents and punctuation.
    /// </summary>
    /// <param name="cast">The name to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<HashSet<int>> LoadCastFilmIdsAsync(string cast, CancellationToken cancellationToken)
    {
        List<Person> people = await db.People.AsNoTracking().Select(x => new Person { PersonId = x.PersonId, Name = x.Name }).ToListAsync(cancellationToken);
        List<int> personIds = [.. people.Where(x => FilterMatch.Contains(x.Name, cast)).Select(x => x.PersonId)];
        return [.. await db.FilmCredits.Where(x => x.Role == CreditRole.Cast && personIds.Contains(x.PersonId)).Select(x => x.FilmId).Distinct().ToListAsync(cancellationToken)];
    }

    /// <summary>
    /// Sorts films by the requested columns, falling back to most recently watched first, then by title.
    /// </summary>
    /// <param name="request">The DataTables request.</param>
    /// <param name="films">The films to sort.</param>
    private static IEnumerable<Film> Sort(DataTablesRequest request, IEnumerable<Film> films)
    {
        return request.Sort(films, data => data switch
        {
            "title" => x => x.Title,
            "year" => x => x.Year ?? int.MinValue,
            "director" => x => x.Director ?? string.Empty,
            "rating" => x => x.FilmId == 0 ? 0 : x.Rating,
            "communityRating" => x => x.CommunityRating ?? double.MinValue,
            "delta" => x => x.FilmId == 0 ? double.MinValue : x.RatingDelta ?? double.MinValue,
            _ => x => x.WatchedOn ?? DateOnly.MinValue
        })?.ThenBy(x => x.Title) ?? films.OrderByDescending(x => x.WatchedOn ?? DateOnly.MinValue).ThenBy(x => x.Title);
    }

    /// <summary>
    /// Builds a table row for a film, including its poster and link.
    /// </summary>
    /// <param name="film">The film; unrated library films have a film ID of 0.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<FilmRow> ToRowAsync(Film film, CancellationToken cancellationToken)
    {
        bool rated = film.FilmId != 0;
        string url = (rated ? Url.Page("/Films/Details", new { id = film.FilmId }) : Url.Page("/Films/Preview", new { tmdbId = film.TMDbId })) ?? string.Empty;
        string? posterUrl = await posters.UrlAsync(film.TMDbId, film.IMDbId, film.PosterPath, 92, cancellationToken);
        return new FilmRow { Url = url, PosterUrl = posterUrl, Title = film.Title, Year = film.Year, Director = film.Director, Rating = rated ? film.Rating : null, RatingColour = rated ? RatingColour.ForRating(film.Rating) : null, CommunityRating = film.CommunityRating, CommunityRatingColour = film.CommunityRating is double communityRating ? RatingColour.ForRating(communityRating) : null, Delta = rated ? film.RatingDelta : null, WatchedOn = film.WatchedOn };
    }
}