using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Models.Jellyfin;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// On a schedule, refreshes films' external ratings oldest first, then re-pushes any of my ratings missing or changed in Jellyfin.
/// </summary>
/// <param name="scopeFactory">Creates a scope for each run.</param>
/// <param name="options">The refresh options.</param>
/// <param name="logger">The logger.</param>
public partial class RatingsRefreshService(IServiceScopeFactory scopeFactory, IOptions<RatingsRefreshOptions> options, ILogger<RatingsRefreshService> logger) : BackgroundService
{
    /// <summary>
    /// Runs the refresh and Jellyfin check at startup and then on every interval until the app stops.
    /// </summary>
    /// <param name="stoppingToken">Signals that the app is stopping.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(options.Value.Interval);
        do
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogRefreshFailed(logger, ex);
            }
            try
            {
                await RepushJellyfinAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogRepushFailed(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Sets the critics rating in Jellyfin to my rating for every film where it is missing or different.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task RepushJellyfinAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        MMDbContext db = scope.ServiceProvider.GetRequiredService<MMDbContext>();
        JellyfinClient jellyfin = scope.ServiceProvider.GetRequiredService<JellyfinClient>();
        List<Film> films = await db.Films.AsNoTracking().ToListAsync(cancellationToken);
        Dictionary<string, Film> byIMDbId = films.Where(x => x.IMDbId is not null).GroupBy(x => x.IMDbId!, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        Dictionary<int, Film> byTMDbId = films.Where(x => x.TMDbId is not null).GroupBy(x => x.TMDbId!.Value).ToDictionary(x => x.Key, x => x.First());
        List<JellyfinItem> items = await jellyfin.GetMoviesAsync(cancellationToken);
        int repushed = 0;
        foreach (JellyfinItem item in items)
        {
            Film? film = null;
            if (item.IMDbId is string imdbId)
            {
                byIMDbId.TryGetValue(imdbId, out film);
            }
            if (film is null && item.TMDbId is int tmdbId)
            {
                byTMDbId.TryGetValue(tmdbId, out film);
            }
            if (film is null || item.CriticRating == film.Rating)
            {
                continue;
            }
            await jellyfin.SetCriticRatingAsync(item.Id, film.Rating, cancellationToken);
            repushed++;
        }
        LogRepushed(logger, repushed);
    }

    /// <summary>
    /// Refreshes a batch of films whose ratings are missing or older than the maximum age.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        MMDbContext db = scope.ServiceProvider.GetRequiredService<MMDbContext>();
        FilmDataService filmData = scope.ServiceProvider.GetRequiredService<FilmDataService>();
        DateTime cutoff = DateTime.UtcNow - options.Value.MaxAge;
        List<Film> films = await db.Films.Where(x => x.IMDbId != null && (x.RatingsUpdatedAt == null || x.RatingsUpdatedAt < cutoff)).OrderBy(x => x.RatingsUpdatedAt != null).ThenBy(x => x.RatingsUpdatedAt).Take(options.Value.BatchSize).ToListAsync(cancellationToken);
        int due = films.Count;
        int refreshed = 0;
        foreach (Film film in films)
        {
            try
            {
                await filmData.RefreshRatingsAsync(film, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                LogRefreshStopped(logger, ex, film.Title);
                break;
            }
            refreshed++;
            if (refreshed % 25 == 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        LogRefreshed(logger, refreshed, due);
    }

    /// <summary>
    /// Logs that a refresh run failed unexpectedly.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Ratings refresh failed.")]
    private static partial void LogRefreshFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs that a refresh run stopped early because OMDb could not be reached.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="title">The film being refreshed when it stopped.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Stopping ratings refresh at {Title}; OMDb is unavailable or the daily limit was reached.")]
    private static partial void LogRefreshStopped(ILogger logger, Exception exception, string title);

    /// <summary>
    /// Logs how many films a refresh run updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="refreshed">The number of films refreshed.</param>
    /// <param name="due">The number of films that were due.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Refreshed ratings for {Refreshed} of {Due} films.")]
    private static partial void LogRefreshed(ILogger logger, int refreshed, int due);

    /// <summary>
    /// Logs that the Jellyfin check failed unexpectedly.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    [LoggerMessage(Level = LogLevel.Error, Message = "Jellyfin rating check failed.")]
    private static partial void LogRepushFailed(ILogger logger, Exception exception);

    /// <summary>
    /// Logs how many ratings were re-pushed to Jellyfin.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="repushed">The number of films updated in Jellyfin.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Re-pushed {Repushed} ratings to Jellyfin.")]
    private static partial void LogRepushed(ILogger logger, int repushed);
}