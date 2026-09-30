using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Models.OMDb;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Serves OMDb ratings from a database cache, fetching only entries that are missing or older than the cache duration.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="omdb">The OMDb client.</param>
/// <param name="options">The OMDb options.</param>
/// <param name="logger">The logger.</param>
public partial class OMDbCacheService(MMDbContext db, OMDbClient omdb, IOptions<OMDbOptions> options, ILogger<OMDbCacheService> logger)
{
    /// <summary>
    /// Gets cached ratings for several films, fetching missing or stale ones from OMDb in parallel; if OMDb fails, stale entries are kept and missing ones are left out.
    /// </summary>
    /// <param name="imdbIds">The IMDb IDs to look up.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Dictionary<string, OMDbCacheEntry>> GetManyAsync(IEnumerable<string> imdbIds, CancellationToken cancellationToken = default)
    {
        List<string> ids = [.. imdbIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)];
        Dictionary<string, OMDbCacheEntry> entries = await db.OMDbCache.Where(x => ids.Contains(x.IMDbId)).ToDictionaryAsync(x => x.IMDbId, StringComparer.OrdinalIgnoreCase, cancellationToken);
        DateTime cutoff = DateTime.UtcNow - options.Value.CacheDuration;
        List<string> stale = [.. ids.Where(x => !entries.TryGetValue(x, out OMDbCacheEntry? entry) || entry.FetchedAt < cutoff)];
        (string IMDbId, OMDbMovie? Movie, bool Failed)[] fetched = await Task.WhenAll(stale.Select(x => FetchAsync(x, cancellationToken)));
        foreach ((string IMDbId, OMDbMovie? Movie, bool Failed) in fetched.Where(x => !x.Failed))
        {
            if (!entries.TryGetValue(IMDbId, out OMDbCacheEntry? entry))
            {
                entry = new OMDbCacheEntry { IMDbId = IMDbId };
                db.OMDbCache.Add(entry);
                entries[IMDbId] = entry;
            }
            entry.Year = Movie?.YearValue;
            entry.IMDbRating = Movie?.ImdbRatingValue;
            entry.IMDbVotes = Movie?.ImdbVotesValue;
            entry.RottenTomatoes = Movie?.RottenTomatoesValue;
            entry.Metacritic = Movie?.MetacriticValue;
            entry.FetchedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        return entries;
    }

    /// <summary>
    /// Gets cached ratings for one film, returning null if there are none and OMDb could not be reached.
    /// </summary>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<OMDbCacheEntry?> GetAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        Dictionary<string, OMDbCacheEntry> entries = await GetManyAsync([imdbId], cancellationToken);
        return entries.GetValueOrDefault(imdbId);
    }

    /// <summary>
    /// Fetches one film from OMDb, flagging a failure such as the daily limit instead of throwing.
    /// </summary>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<(string IMDbId, OMDbMovie? Movie, bool Failed)> FetchAsync(string imdbId, CancellationToken cancellationToken)
    {
        try
        {
            return (imdbId, await omdb.GetByImdbIdAsync(imdbId, cancellationToken), false);
        }
        catch (HttpRequestException ex)
        {
            LogFetchFailed(logger, ex, imdbId);
            return (imdbId, null, true);
        }
    }

    /// <summary>
    /// Logs that OMDb could not be reached for a film, so the cached or TMDb-only rating is used instead.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="imdbId">The IMDb ID.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not fetch OMDb ratings for {IMDbId}; falling back to cached or TMDb-only ratings.")]
    private static partial void LogFetchFailed(ILogger logger, Exception exception, string imdbId);
}