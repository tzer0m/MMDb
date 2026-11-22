namespace MMDb.Models;

/// <summary>
/// Where a film's current placement has got to, worked out from its answers since the placement started.
/// </summary>
public class RankingState
{
    /// <summary>
    /// The score it must be above: the best film it beat, or the bottom of the range.
    /// </summary>
    public double Lower { get; set; }

    /// <summary>
    /// The score it must be below: the worst film it lost to, or the top of the range.
    /// </summary>
    public double Upper { get; set; }

    /// <summary>
    /// The film that set the lower bound, if a film did.
    /// </summary>
    public int? LowerFilmId { get; set; }

    /// <summary>
    /// The film that set the upper bound, if a film did.
    /// </summary>
    public int? UpperFilmId { get; set; }

    /// <summary>
    /// How many answers count towards settling, leaving out skips.
    /// </summary>
    public int Comparisons { get; set; }

    /// <summary>
    /// The films already asked about, including skipped ones, so they aren't asked again.
    /// </summary>
    public HashSet<int> Asked { get; set; } = [];

    /// <summary>
    /// The most recent answer, if there is one.
    /// </summary>
    public ComparisonResult? LastResult { get; set; }

    /// <summary>
    /// How many answers to questions double-checking a big move count so far, leaving out skips.
    /// </summary>
    public int Checks { get; set; }

    /// <summary>
    /// Whether the most recent answer was to a question double-checking a big move.
    /// </summary>
    public bool LastWasCheck { get; set; }

    /// <summary>
    /// Whether the answers contradict each other: it beat a film ranked above one it lost to.
    /// </summary>
    public bool IsContradiction => Lower >= Upper;

    /// <summary>
    /// Works out the state from a film's answers in order; similar counts as better, except straight after another similar, when it counts as worse so the film goes between the two.
    /// </summary>
    /// <param name="score">The film's score.</param>
    /// <param name="answers">Its answers since the placement started, oldest first.</param>
    /// <param name="scores">Every film's score, keyed by film ID.</param>
    public static RankingState Build(FilmScore score, List<FilmComparison> answers, Dictionary<int, FilmScore> scores)
    {
        RankingState state = score.IsAnchor ? new RankingState { Lower = (score.Band - 1) * 10, Upper = score.Band * 10 } : new RankingState { Lower = 0, Upper = 100 };
        bool previousSimilar = false;
        foreach (FilmComparison answer in answers)
        {
            state.Asked.Add(answer.OtherFilmId);
            state.LastResult = answer.Result;
            state.LastWasCheck = answer.IsCheck;
            if (answer.Result == ComparisonResult.Skipped || !scores.TryGetValue(answer.OtherFilmId, out FilmScore? other))
            {
                continue;
            }
            state.Comparisons++;
            state.Checks += answer.IsCheck ? 1 : 0;
            bool above = answer.Result == ComparisonResult.Better || answer.Result == ComparisonResult.Similar && !previousSimilar;
            if (above && other.Score > state.Lower)
            {
                state.Lower = other.Score;
                state.LowerFilmId = other.FilmId;
            }
            else if (!above && other.Score < state.Upper)
            {
                state.Upper = other.Score;
                state.UpperFilmId = other.FilmId;
            }
            previousSimilar = answer.Result == ComparisonResult.Similar;
        }
        return state;
    }
}