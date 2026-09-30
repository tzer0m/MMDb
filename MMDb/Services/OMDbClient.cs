using System.Text.Json;
using Microsoft.Extensions.Options;
using MMDb.Models.OMDb;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Client for the OMDb API.
/// </summary>
/// <param name="http">The HTTP client.</param>
/// <param name="options">The OMDb options.</param>
public class OMDbClient(HttpClient http, IOptions<OMDbOptions> options)
{
    /// <summary>
    /// JSON options for OMDb's mixed-case property names.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Gets a movie's ratings by its IMDb ID, returning null if OMDb has no match.
    /// </summary>
    /// <param name="imdbId">The IMDb ID, e.g. tt0113277.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<OMDbMovie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken = default)
    {
        string url = $"{options.Value.BaseUrl}?i={Uri.EscapeDataString(imdbId)}&apikey={Uri.EscapeDataString(options.Value.ApiKey)}";
        using HttpResponseMessage response = await http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        OMDbMovie? movie = await response.Content.ReadFromJsonAsync<OMDbMovie>(JsonOptions, cancellationToken);
        return movie is { Found: true } ? movie : null;
    }
}