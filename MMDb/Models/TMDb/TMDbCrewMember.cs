namespace MMDb.Models.TMDb;

/// <summary>
/// A crew member of a TMDb movie.
/// </summary>
public class TMDbCrewMember
{
    /// <summary>
    /// The crew member's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The crew member's job, e.g. Director.
    /// </summary>
    public string? Job { get; set; }
}