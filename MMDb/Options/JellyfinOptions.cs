namespace MMDb.Options;

/// <summary>
/// Configuration for the Jellyfin API.
/// </summary>
public class JellyfinOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Jellyfin";

    /// <summary>
    /// The base URL of the Jellyfin server.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The Jellyfin API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the Jellyfin user whose watch history is read.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}