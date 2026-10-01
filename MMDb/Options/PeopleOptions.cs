namespace MMDb.Options;

/// <summary>
/// Configuration for person pages.
/// </summary>
public class PeopleOptions
{
    /// <summary>
    /// How old a person's cached details can be before they are fetched again from TMDb.
    /// </summary>
    public TimeSpan DetailsMaxAge { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How many top-billed cast members to store and show for each film.
    /// </summary>
    public int CastCount { get; set; } = 11;

    /// <summary>
    /// How many of a person's top rated films to show.
    /// </summary>
    public int TopRatedCount { get; set; } = 10;

    /// <summary>
    /// The fewest TMDb votes a film needs to count towards a person's top rated films.
    /// </summary>
    public int TopRatedMinVotes { get; set; } = 500;

    /// <summary>
    /// How long a person's TMDb film credits are cached in memory.
    /// </summary>
    public TimeSpan CreditsCacheDuration { get; set; } = TimeSpan.FromDays(1);
}