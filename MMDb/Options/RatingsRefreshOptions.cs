namespace MMDb.Options;

/// <summary>
/// Configuration for the background job that refreshes external ratings.
/// </summary>
public class RatingsRefreshOptions
{
    /// <summary>
    /// How often the job runs.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// How old a film's ratings can be before they are refreshed.
    /// </summary>
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// The most films to refresh in a single run, to stay within the OMDb daily limit.
    /// </summary>
    public int BatchSize { get; set; } = 500;
}