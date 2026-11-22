namespace MMDb.Models;

/// <summary>
/// The next step in ranking a film: the film to compare it with, or a message if its placement has finished.
/// </summary>
public class RankingQuestion
{
    /// <summary>
    /// The film being ranked.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The film to compare it with, or null if there is nothing left to ask.
    /// </summary>
    public Film? Opponent { get; set; }

    /// <summary>
    /// How many answers count towards settling so far.
    /// </summary>
    public int Comparisons { get; set; }

    /// <summary>
    /// How many answers it needs before it can settle.
    /// </summary>
    public int ComparisonsNeeded { get; set; }

    /// <summary>
    /// Whether the film is an anchor.
    /// </summary>
    public bool IsAnchor { get; set; }

    /// <summary>
    /// Whether this question double-checks a big move away from the film's original rating.
    /// </summary>
    public bool IsCheck { get; set; }

    /// <summary>
    /// What happened when the placement finished, e.g. the score it settled at.
    /// </summary>
    public string? Message { get; set; }
}