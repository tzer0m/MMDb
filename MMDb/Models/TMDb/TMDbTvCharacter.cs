namespace MMDb.Models.TMDb;

/// <summary>
/// A character an actor played on a TMDb TV show.
/// </summary>
public class TMDbTvCharacter
{
    /// <summary>
    /// The character's name.
    /// </summary>
    public string? Character { get; set; }

    /// <summary>
    /// How many episodes they played this character in.
    /// </summary>
    public int EpisodeCount { get; set; }
}