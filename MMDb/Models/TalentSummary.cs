namespace MMDb.Models;

/// <summary>
/// A director or actor with totals across the films of theirs I have rated.
/// </summary>
public class TalentSummary
{
    /// <summary>
    /// The TMDb person ID.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// The person's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The TMDb profile photo path.
    /// </summary>
    public string? ProfilePath { get; set; }

    /// <summary>
    /// How many of their films I have rated.
    /// </summary>
    public int FilmCount { get; set; }

    /// <summary>
    /// What they did across those films: D for director, A for actor, or D/A for both.
    /// </summary>
    public string Roles { get; set; } = string.Empty;

    /// <summary>
    /// My average rating across their films.
    /// </summary>
    public double AverageMyRating { get; set; }

    /// <summary>
    /// The average community rating across their films that have one.
    /// </summary>
    public double? AverageCommunityRating { get; set; }
}