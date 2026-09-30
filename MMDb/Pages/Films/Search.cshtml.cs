using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Models.TMDb;
using MMDb.Services;

namespace MMDb.Pages.Films;

/// <summary>
/// Searches TMDb for films, flagging the ones I have already rated.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="tmdb">The TMDb client.</param>
/// <param name="logger">The logger.</param>
public partial class SearchModel(MMDbContext db, TMDbClient tmdb, ILogger<SearchModel> logger) : PageModel
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
    /// Searches TMDb when a query is given, otherwise shows just the search box.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Query))
        {
            return;
        }
        Results = await tmdb.SearchAsync(Query.Trim(), cancellationToken);
        List<int> ids = [.. Results.Select(x => x.Id)];
        TMDbMovie?[] details = await Task.WhenAll(ids.Select(x => TryGetMovieAsync(x, cancellationToken)));
        Directors = details.OfType<TMDbMovie>().ToDictionary(x => x.Id, x => string.Join(", ", (x.Credits?.Crew ?? []).Where(c => c.Job == "Director").Select(c => c.Name).Distinct()));
        ExistingFilms = await db.Films.Where(x => x.TMDbId != null && ids.Contains(x.TMDbId.Value)).AsNoTracking().ToDictionaryAsync(x => x.TMDbId!.Value, cancellationToken);
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
}