using Microsoft.Extensions.Options;
using MMDb.Models.Jellyfin;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Picks the poster URL for a film: my Jellyfin poster through the poster endpoint when the film is in the library, otherwise the TMDb poster.
/// </summary>
/// <param name="jellyfin">The Jellyfin client.</param>
/// <param name="tmdbOptions">The TMDb options.</param>
/// <param name="logger">The logger.</param>
public partial class PosterService(JellyfinClient jellyfin, IOptions<TMDbOptions> tmdbOptions, ILogger<PosterService> logger)
{
    /// <summary>
    /// Library items with a poster, keyed by TMDb ID, loaded once per request.
    /// </summary>
    private Dictionary<int, JellyfinItem>? ByTMDbId;

    /// <summary>
    /// Library items with a poster, keyed by IMDb ID, loaded once per request.
    /// </summary>
    private Dictionary<string, JellyfinItem>? ByIMDbId;

    /// <summary>
    /// Gets the poster URL for a film, or null if neither Jellyfin nor TMDb has one.
    /// </summary>
    /// <param name="tmdbId">The TMDb ID.</param>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="posterPath">The TMDb poster path, used as the fallback.</param>
    /// <param name="width">The width in pixels; one of the TMDb poster sizes, e.g. 92, 185 or 342.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<string?> UrlAsync(int? tmdbId, string? imdbId, string? posterPath, int width, CancellationToken cancellationToken = default)
    {
        await LoadAsync(cancellationToken);
        JellyfinItem? item = (tmdbId is int tmdb ? ByTMDbId!.GetValueOrDefault(tmdb) : null) ?? (imdbId is string imdb ? ByIMDbId!.GetValueOrDefault(imdb) : null);
        if (item?.PrimaryImageTag is string tag)
        {
            return $"/poster/{item.Id}/{tag}?w={width}";
        }
        return posterPath is null ? null : $"{tmdbOptions.Value.ImageBaseUrl}w{width}{posterPath}";
    }

    /// <summary>
    /// Loads the library from Jellyfin's cached list the first time it is needed, leaving it empty if Jellyfin can't be reached.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        if (ByTMDbId is not null)
        {
            return;
        }
        List<JellyfinItem> movies = [];
        try
        {
            movies = await jellyfin.GetCachedMoviesAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            LogLibraryFailed(logger, ex);
        }
        List<JellyfinItem> withPosters = [.. movies.Where(x => x.PrimaryImageTag is not null)];
        ByTMDbId = withPosters.Where(x => x.TMDbId is not null).GroupBy(x => x.TMDbId!.Value).ToDictionary(x => x.Key, x => x.First());
        ByIMDbId = withPosters.Where(x => x.IMDbId is not null).GroupBy(x => x.IMDbId!, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Logs that the Jellyfin library could not be loaded, so TMDb posters are used.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not load the Jellyfin library; using TMDb posters.")]
    private static partial void LogLibraryFailed(ILogger logger, Exception exception);
}