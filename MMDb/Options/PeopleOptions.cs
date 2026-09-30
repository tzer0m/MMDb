namespace MMDb.Options;

/// <summary>
/// Configuration for cached person details.
/// </summary>
public class PeopleOptions
{
    /// <summary>
    /// How old a person's cached details can be before they are fetched again from TMDb.
    /// </summary>
    public TimeSpan DetailsMaxAge { get; set; } = TimeSpan.FromDays(30);
}