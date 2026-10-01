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

namespace MMDb.Pages.Films;

/// <summary>
/// Searches TMDb for films, flagging the ones I have already rated.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="omdbCache">The cached OMDb ratings.</param>
/// <param name="jellyfin">The Jellyfin client, for library flags.</param>
/// <param name="options">The search options.</param>
/// <param name="logger">The logger.</param>
public partial class SearchModel(MMDbContext db, TMDbClient tmdb, OMDbCacheService omdbCache, JellyfinClient jellyfin, IOptions<SearchOptions> options, ILogger<SearchModel> logger) : PageModel
{
    /// <summary>
    /// The title to search TMDb for.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <summary>
    /// The TMDb search results.
    /// </summary>
    public List<TMDbSearchResult> Results { get; set; } = [];

    /// <summary>
    /// Films already in MMDb, keyed by TMDb ID.
    /// </summary>
    public Dictionary<int, Film> ExistingFilms { get; set; } = [];

    /// <summary>
    /// The director names for each search result, keyed by TMDb ID.
    /// </summary>
    public Dictionary<int, string> Directors { get; set; } = [];

    /// <summary>
    /// The community rating for each search result, keyed by TMDb ID, falling back to the TMDb rating alone when OMDb has nothing.
    /// </summary>
    public Dictionary<int, double> CommunityRatings { get; set; } = [];

    /// <summary>
    /// Which ratings went into each community rating, keyed by TMDb ID, shown as the badge tooltip.
    /// </summary>
    public Dictionary<int, string> RatingSources { get; set; } = [];

    /// <summary>
    /// The TMDb IDs of films in my Jellyfin library, for the library flag.
    /// </summary>
    public HashSet<int> Library { get; set; } = [];

    /// <summary>
    /// Searches TMDb when a query is given, otherwise shows just the search box.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return;
        }
        Results = [.. (await tmdb.SearchAsync(Query.Trim(), cancellationToken)).Take(options.Value.MaxResults)];
        List<int> ids = [.. Results.Select(x => x.Id)];
        TMDbMovie?[] details = await Task.WhenAll(ids.Select(x => TryGetMovieAsync(x, cancellationToken)));
        Directors = details.OfType<TMDbMovie>().ToDictionary(x => x.Id, x => string.Join(", ", (x.Credits?.Crew ?? []).Where(c => c.Job == "Director").Select(c => c.Name).Distinct()));
        ExistingFilms = await db.Films.Where(x => x.TMDbId != null && ids.Contains(x.TMDbId.Value)).AsNoTracking().ToDictionaryAsync(x => x.TMDbId!.Value, cancellationToken);
        try
        {
            Library = await jellyfin.GetLibraryTMDbIdsAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogLibraryFailed(logger, ex);
        }
        Dictionary<int, TMDbMovie> movies = details.OfType<TMDbMovie>().ToDictionary(x => x.Id);
        Dictionary<string, OMDbCacheEntry> ratings = await omdbCache.GetManyAsync(movies.Values.Where(x => !ExistingFilms.ContainsKey(x.Id) && !string.IsNullOrWhiteSpace(x.ImdbId)).Select(x => x.ImdbId!), cancellationToken);
        foreach (TMDbSearchResult result in Results)
        {
            if (ExistingFilms.TryGetValue(result.Id, out Film? film))
            {
                AddCommunityRating(result.Id, film.TMDbRating, film.IMDbRating, film.RottenTomatoes, film.Metacritic);
            }
            else
            {
                TMDbMovie? movie = movies.GetValueOrDefault(result.Id);
                double? tmdbRating = movie is { VoteCount: > 0 } ? movie.VoteAverage : result.VoteCount > 0 ? result.VoteAverage : null;
                OMDbCacheEntry? entry = movie?.ImdbId is string imdbId ? ratings.GetValueOrDefault(imdbId) : null;
                AddCommunityRating(result.Id, tmdbRating, entry?.IMDbRating, entry?.RottenTomatoes, entry?.Metacritic);
            }
        }
    }

    /// <summary>
    /// Records a search result's community rating and its sources, if it has any ratings at all.
    /// </summary>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    /// <param name="tmdbRating">The TMDb rating, out of 10.</param>
    /// <param name="imdbRating">The IMDb rating, out of 10.</param>
    /// <param name="rottenTomatoes">The Rotten Tomatoes score, as a percentage.</param>
    /// <param name="metacritic">The Metacritic score, out of 100.</param>
    private void AddCommunityRating(int tmdbId, double? tmdbRating, double? imdbRating, int? rottenTomatoes, int? metacritic)
    {
        if (CommunityRatingCalculator.Calculate(tmdbRating, imdbRating, rottenTomatoes, metacritic) is double rating)
        {
            CommunityRatings[tmdbId] = rating;
            RatingSources[tmdbId] = CommunityRatingCalculator.Describe(tmdbRating, imdbRating, rottenTomatoes, metacritic);
        }
    }

    /// <summary>
    /// Gets a movie's details from TMDb, returning null instead of throwing if the request fails.
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
            LogDetailsFailed(logger, ex, tmdbId);
            return null;
        }
    }

    /// <summary>
    /// Logs that TMDb details could not be loaded for a search result.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not load TMDb details for {TMDbId}.")]
    private static partial void LogDetailsFailed(ILogger logger, Exception exception, int tmdbId);

    /// <summary>
    /// Logs that the Jellyfin library could not be loaded, so no library flags are shown.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not load the Jellyfin library; showing no library flags.")]
    private static partial void LogLibraryFailed(ILogger logger, Exception exception);
}