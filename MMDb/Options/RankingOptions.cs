namespace MMDb.Options;

/// <summary>
/// Configuration for ranking films by comparing them.
/// </summary>
public class RankingOptions
{
    /// <summary>
    /// The fewest answers a film needs before its score can settle; anchors don't need any.
    /// </summary>
    public int MinComparisons { get; set; } = 5;

    /// <summary>
    /// How close together, in points, the films either side must be for the score to settle.
    /// </summary>
    public double SettledRange { get; set; } = 3;

    /// <summary>
    /// How many answers make up one ranking session.
    /// </summary>
    public int SessionLength { get; set; } = 10;
}