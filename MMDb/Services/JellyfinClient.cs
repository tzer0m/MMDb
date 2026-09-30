using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using MMDb.Models.Jellyfin;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Client for the Jellyfin API.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">The Jellyfin options.</param>
public class JellyfinClient(HttpClient http, IOptions<JellyfinOptions> options)
{
    // ONE-TIME IMPORT: remove this method once the Jellyfin/IMDb import is complete.
    /// <summary>
    /// Gets every movie the configured user has played.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<JellyfinItem>> GetPlayedMoviesAsync(CancellationToken cancellationToken = default)
    {
        return await GetMoviesAsync(true, cancellationToken);
    }

    /// <summary>
    /// Finds a movie in the library by IMDb ID, falling back to TMDb ID.
    /// </summary>
    /// <param name="imdbId">The IMDb ID.</param>
    /// <param name="tmdbId">The TMDb ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JellyfinItem?> FindMovieAsync(string? imdbId, int? tmdbId, CancellationToken cancellationToken = default)
    {
        List<JellyfinItem> movies = await GetMoviesAsync(null, cancellationToken);
        return movies.FirstOrDefault(x => imdbId is not null && string.Equals(x.IMDbId, imdbId, StringComparison.OrdinalIgnoreCase)) ?? movies.FirstOrDefault(x => tmdbId is not null && x.TMDbId == tmdbId);
    }

    /// <summary>
    /// Sets a movie's community rating, leaving all other metadata unchanged.
    /// </summary>
    /// <param name="itemId">The Jellyfin item ID.</param>
    /// <param name="rating">The rating, from 1 to 10.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetCommunityRatingAsync(string itemId, int rating, CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage getRequest = CreateRequest(HttpMethod.Get, $"Items/{itemId}?userId={options.Value.UserId}");
        using HttpResponseMessage getResponse = await http.SendAsync(getRequest, cancellationToken);
        getResponse.EnsureSuccessStatusCode();
        JsonObject item = await getResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken) ?? throw new InvalidOperationException($"Jellyfin returned no metadata for item {itemId}.");
        item["CommunityRating"] = rating;
        using HttpRequestMessage postRequest = CreateRequest(HttpMethod.Post, $"Items/{itemId}");
        postRequest.Content = JsonContent.Create(item);
        using HttpResponseMessage postResponse = await http.SendAsync(postRequest, cancellationToken);
        postResponse.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets all movies in the library with their provider IDs, optionally filtered by played state.
    /// </summary>
    /// <param name="isPlayed">True for played only, false for unplayed only, or null for all.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<List<JellyfinItem>> GetMoviesAsync(bool? isPlayed, CancellationToken cancellationToken)
    {
        string path = $"Items?userId={options.Value.UserId}&includeItemTypes=Movie&recursive=true&fields=ProviderIds";
        if (isPlayed is bool played)
        {
            path += $"&isPlayed={played.ToString().ToLowerInvariant()}";
        }
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