namespace MMDb.Options;

/// <summary>
/// Configuration for the background jobs that refresh external ratings and keep Jellyfin's ratings in step.
/// </summary>
public class RatingsRefreshOptions
{
    /// <summary>
    /// How often external ratings are refreshed and the library list is synced.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// How often Jellyfin's ratings are checked and any that differ are pushed again, e.g. after Jellyfin overwrites them.
    /// </summary>
    public TimeSpan JellyfinInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How old a film's ratings can be before they are refreshed.
    /// </summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// The most films to refresh in a single run, to stay within the OMDb daily limit.
    /// </summary>
    public int BatchSize { get; set; } = 500;
}