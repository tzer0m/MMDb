namespace MMDb.Models;

/// <summary>
/// A TV show a person appeared in, shown on their page.
/// </summary>
public class TvRole
{
    /// <summary>
    /// The TMDb TV show ID.
    /// </summary>
    public int TMDbId { get; set; }

    /// <summary>
    /// The show's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The year the show first aired.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The TMDb poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The character or characters they played.
    /// </summary>
    public string Character { get; set; } = string.Empty;

    /// <summary>
    /// How many episodes they appeared in.
    /// </summary>
    public int EpisodeCount { get; set; }
}