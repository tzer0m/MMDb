namespace MMDb.Models.TMDb;

/// <summary>
/// A cast member of a TMDb movie.
/// </summary>
public class TMDbCastMember
{
    /// <summary>
    /// The actor's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The character played.
    /// </summary>
    public string? Character { get; set; }

    /// <summary>
    /// The billing order, lowest first.
    /// </summary>
    public int Order { get; set; }
}