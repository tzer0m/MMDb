namespace MMDb.Models.TMDb;

/// <summary>
/// A TMDb genre.
/// </summary>
public class TMDbGenre
{
    /// <summary>
    /// The TMDb genre ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The genre name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}