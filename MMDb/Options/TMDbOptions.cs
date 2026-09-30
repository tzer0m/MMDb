namespace MMDb.Options;

/// <summary>
/// Configuration for the TMDb API.
/// </summary>
public class TMDbOptions
{
    /// <summary>
    /// The base URL of the TMDb API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The base URL for TMDb images, before the size segment.
    /// </summary>
    public string ImageBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The TMDb API read access token.
    /// </summary>
    public string ApiReadAccessToken { get; set; } = string.Empty;
}