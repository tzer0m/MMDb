namespace MMDb.Models.TMDb;

/// <summary>
/// A cast member of a TMDb TV show across every season.
/// </summary>
public class TMDbAggregateCastMember
{
    /// <summary>
    /// The TMDb person ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The actor's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The profile photo path.
    /// </summary>
    public string? ProfilePath { get; set; }

    /// <summary>
    /// The characters they played.
    /// </summary>
    public List<TMDbTvCharacter> Roles { get; set; } = [];

    /// <summary>
    /// How many episodes they appeared in.
    /// </summary>
    public int TotalEpisodeCount { get; set; }

    /// <summary>
    /// The billing order, lowest first.
    /// </summary>
    public int Order { get; set; }
}