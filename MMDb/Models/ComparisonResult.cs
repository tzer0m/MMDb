namespace MMDb.Models;

/// <summary>
/// My answer when comparing the film being ranked with another film.
/// </summary>
public enum ComparisonResult
{
    /// <summary>
    /// The film being ranked is better than the other film.
    /// </summary>
    Better,

    /// <summary>
    /// The film being ranked is worse than the other film.
    /// </summary>
    Worse,

    /// <summary>
    /// The two films are about the same.
    /// </summary>
    Similar,

    /// <summary>
    /// I couldn't remember the other film well enough to compare.
    /// </summary>
    Skipped
}