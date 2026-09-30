namespace MMDb.Options;

/// <summary>
/// Configuration for the OMDb API.
/// </summary>
public class OMDbOptions
{
    /// <summary>
    /// The base URL of the OMDb API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The OMDb API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// How long cached OMDb ratings are used by searches and previews before being fetched again.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromDays(30);
}