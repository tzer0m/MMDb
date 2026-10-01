namespace MMDb.Options;

/// <summary>
/// Configuration for the Jellyfin API.
/// </summary>
public class JellyfinOptions
{
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

    /// <summary>
    /// How long the list of films in the Jellyfin library is cached when checking whether a film is there.
    /// </summary>
    public TimeSpan LibraryCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long the list of collections and their movies is cached.
    /// </summary>
    public TimeSpan CollectionsCacheDuration { get; set; } = TimeSpan.FromHours(1);
}