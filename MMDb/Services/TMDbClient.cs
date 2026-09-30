using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MMDb.Models.TMDb;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Client for the TMDb API.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">The TMDb options.</param>
public class TMDbClient(HttpClient http, IOptions<TMDbOptions> options)
{
    /// <summary>
    /// JSON options matching TMDb's snake_case property names.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>
    /// Searches TMDb for movies matching a title.
    /// </summary>
    /// <param name="query">The title to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<List<TMDbSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        TMDbSearchResponse? response = await GetAsync<TMDbSearchResponse>($"search/movie?query={Uri.EscapeDataString(query)}", cancellationToken);
        return response?.Results ?? [];
    }

    /// <summary>
    /// Gets the full details of a movie, including credits.
    /// </summary>
    /// <param name="tmdbId">The TMDb movie ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TMDbMovie?> GetMovieAsync(int tmdbId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TMDbMovie>($"movie/{tmdbId}?append_to_response=credits", cancellationToken);
    }

    /// <summary>
    /// Gets a person's details, returning null if TMDb has no such person.
    /// </summary>
    /// <param name="personId">The TMDb person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<TMDbPerson?> GetPersonAsync(int personId, CancellationToken cancellationToken = default)
    {
        return await GetAsync<TMDbPerson>($"person/{personId}", cancellationToken);
    }

    /// <summary>
    /// Sends an authenticated GET request and deserialises the response, returning null on 404.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="path">The path relative to the API base URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken) where T : class
    {
        using HttpRequestMessage request = new(HttpMethod.Get, options.Value.BaseUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiReadAccessToken);
        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }
}