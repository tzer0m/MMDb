namespace MMDb.Models.TMDb;

/// <summary>
/// A crew member of a TMDb movie.
/// </summary>
public class TMDbCrewMember
{
    /// <summary>
    /// The TMDb person ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The crew member's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The profile photo path.
    /// </summary>
    public string? ProfilePath { get; set; }

    /// <summary>
    /// The crew member's job, e.g. Director.
    /// </summary>
    public string? Job { get; set; }
}