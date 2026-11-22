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
    /// How many bands from its original rating a film must be heading before the move is double-checked.
    /// </summary>
    public int BigMoveBands { get; set; } = 2;

    /// <summary>
    /// How many extra questions double-check a big move.
    /// </summary>
    public int BigMoveChecks { get; set; } = 2;

    /// <summary>
    /// How many recent questions' films to avoid asking about again where there's an alternative.
    /// </summary>
    public int RecentOpponents { get; set; } = 20;
}