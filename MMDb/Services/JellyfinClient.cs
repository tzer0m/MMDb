using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MMDb.Models;
using MMDb.Models.Jellyfin;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Client for the Jellyfin API.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">The Jellyfin options.</param>
/// <param name="cache">The memory cache for the library list.</param>
public class JellyfinClient(HttpClient http, IOptions<JellyfinOptions> options, IMemoryCache cache)
{
    /// <summary>
    /// Finds a movie in the library by IMDb ID, falling back to TMDb ID.
    /// </summary>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="tmdbId">The TMDb ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JellyfinItem?> FindMovieAsync(string? imdbId, int? tmdbId, CancellationToken cancellationToken = default)
    {
        List<JellyfinItem> movies = await GetMoviesAsync(cancellationToken);
        return Match(movies, imdbId, tmdbId);
    }

    /// <summary>
    /// Checks whether a movie is in the library, using a briefly cached copy of the library list.
    /// </summary>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="tmdbId">The TMDb ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<bool> IsInLibraryAsync(string? imdbId, int? tmdbId, CancellationToken cancellationToken = default)
    {
        List<JellyfinItem> movies = await GetCachedMoviesAsync(cancellationToken);
        return Match(movies, imdbId, tmdbId) is not null;
    }

    /// <summary>
    /// Gets the TMDb IDs of every movie in the library, using a briefly cached copy of the library list.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<HashSet<int>> GetLibraryTMDbIdsAsync(CancellationToken cancellationToken = default)
    {
        List<JellyfinItem> movies = await GetCachedMoviesAsync(cancellationToken);
        return [.. movies.Select(x => x.TMDbId).OfType<int>()];
    }

    /// <summary>
    /// Gets the library list from the memory cache, fetching it if needed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<List<JellyfinItem>> GetCachedMoviesAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync("jellyfin-movies", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = options.Value.LibraryCacheDuration;
            return await GetMoviesAsync(cancellationToken);
        }) ?? [];
    }

    /// <summary>
    /// Finds a movie in a list of library items by IMDb ID, falling back to TMDb ID.
    /// </summary>
    /// <param name="movies">The library items.</param>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="tmdbId">The TMDb ID.</param>
    private static JellyfinItem? Match(List<JellyfinItem> movies, string? imdbId, int? tmdbId)
    {
        return movies.FirstOrDefault(x => imdbId is not null && string.Equals(x.IMDbId, imdbId, StringComparison.OrdinalIgnoreCase)) ?? movies.FirstOrDefault(x => tmdbId is not null && x.TMDbId == tmdbId);
    }

    /// <summary>
    /// Pushes a film's rating to its critics rating in Jellyfin, returning false if the film isn't in the library.
    /// </summary>
    /// <param name="film">The film whose rating to push.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<bool> PushRatingAsync(Film film, CancellationToken cancellationToken = default)
    {
        JellyfinItem? item = await FindMovieAsync(film.IMDbId, film.TMDbId, cancellationToken);
        if (item is null)
        {
            return false;
        }
        await SetCriticRatingAsync(item.Id, film.Rating, cancellationToken);
        return true;
    }

    /// <summary>
    /// Sets a movie's critics rating, leaving all other metadata unchanged.
    /// </summary>
    /// <param name="itemId">The Jellyfin item ID.</param>
    /// <param name="rating">The rating, from 1 to 10.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetCriticRatingAsync(string itemId, int rating, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage getRequest = CreateRequest(HttpMethod.Get, $"Items/{itemId}?userId={options.Value.UserId}");
        using HttpResponseMessage getResponse = await http.SendAsync(getRequest, cancellationToken);
        getResponse.EnsureSuccessStatusCode();
        JsonObject item = await getResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken) ?? throw new InvalidOperationException($"Jellyfin returned no metadata for item {itemId}.");
        item["CriticRating"] = rating;
        using HttpRequestMessage postRequest = CreateRequest(HttpMethod.Post, $"Items/{itemId}");
        postRequest.Content = JsonContent.Create(item);
        using HttpResponseMessage postResponse = await http.SendAsync(postRequest, cancellationToken);
        postResponse.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets all movies in the library with their provider IDs and critics ratings.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<JellyfinItem>> GetMoviesAsync(CancellationToken cancellationToken = default)
    {
        string path = $"Items?userId={options.Value.UserId}&includeItemTypes=Movie&recursive=true&fields=ProviderIds";
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, path);
        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        JellyfinItemsResponse? result = await response.Content.ReadFromJsonAsync<JellyfinItemsResponse>(cancellationToken);
        return result?.Items ?? [];
    }

    /// <summary>
    /// Creates a request to the Jellyfin API with the API key attached.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path relative to the server base URL.</param>
    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        HttpRequestMessage request = new(method, $"{options.Value.BaseUrl.TrimEnd('/')}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("MediaBrowser", $"Token=\"{options.Value.ApiKey}\"");
        return request;
    }
}