namespace MMDb.Models.TMDb;

/// <summary>
/// A TV show a person appeared in, from TMDb's person TV credits.
/// </summary>
public class TMDbTvCredit
{
    /// <summary>
    /// The TMDb TV show ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The show's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The first air date as yyyy-MM-dd, or empty if unknown.
    /// </summary>
    public string? FirstAirDate { get; set; }

    /// <summary>
    /// The poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The TMDb genre IDs.
    /// </summary>
    public List<int> GenreIds { get; set; } = [];

    /// <summary>
    /// The number of TMDb user ratings.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The character played.
    /// </summary>
    public string? Character { get; set; }

    /// <summary>
    /// How many episodes they appeared in.
    /// </summary>
    public int EpisodeCount { get; set; }

    /// <summary>
    /// The year the show first aired, parsed from the first air date.
    /// </summary>
    public int? Year => FirstAirDate is { Length: >= 4 } && int.TryParse(FirstAirDate[..4], out int year) ? year : null;
}