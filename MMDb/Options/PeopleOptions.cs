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
    /// How many top-billed cast members to show for each film; every cast member is stored.
    /// </summary>
    public int CastCount { get; set; } = 11;

    /// <summary>
    /// How many of a person's top rated films to show.
    /// </summary>
    public int TopRatedCount { get; set; } = 10;

    /// <summary>
    /// How high an actor must be billed for a film to count as a lead role, e.g. 3 for the top 3.
    /// </summary>
    public int LeadCount { get; set; } = 3;

    /// <summary>
    /// The fewest TMDb votes a film needs to count towards a person's top rated films.
    /// </summary>
    public int TopRatedMinVotes { get; set; } = 500;

    /// <summary>
    /// How long a person's TMDb film and TV credits are cached in memory.
    /// </summary>
    public TimeSpan CreditsCacheDuration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// How many of a person's TV shows to show.
    /// </summary>
    public int TvCount { get; set; } = 10;

    /// <summary>
    /// TMDb TV genre IDs left out of a person's TV shows, e.g. news, reality and talk shows.
    /// </summary>
    public List<int> TvExcludedGenreIds { get; set; } = [];
}