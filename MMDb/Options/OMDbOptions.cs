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
}