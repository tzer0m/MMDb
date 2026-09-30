namespace MMDb.Models.TMDb;

/// <summary>
/// A cast member of a TMDb movie.
/// </summary>
public class TMDbCastMember
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
    /// The character played.
    /// </summary>
    public string? Character { get; set; }

    /// <summary>
    /// The billing order, lowest first.
    /// </summary>
    public int Order { get; set; }
}