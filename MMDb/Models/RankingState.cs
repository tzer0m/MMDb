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
    /// Whether the answers contradict each other: it beat a film ranked above one it lost to.
    /// </summary>
    public bool IsContradiction => Lower >= Upper;

    /// <summary>
    /// Works out the state from a film's answers in order. Similar only narrows the range when the other film is inside it: the first time it counts as better, and straight after another similar it counts as worse, so the film goes between the two. An answer that only clashes with a film within the clash tolerance of it is left out, since that's noise between near-identical films rather than a real contradiction.
    /// </summary>
    /// <param name="answers">Its answers since the placement started, oldest first.</param>
    /// <param name="scores">Every film's score, keyed by film ID.</param>
    /// <param name="clashTolerance">How close, in shown points, two films must be for a clash between them to be ignored.</param>
    public static RankingState Build(List<FilmComparison> answers, Dictionary<int, FilmScore> scores, double clashTolerance)
    {
        RankingState state = new() { Lower = 0, Upper = 100 };
        bool previousSimilar = false;
        foreach (FilmComparison answer in answers)
        {
            state.Asked.Add(answer.OtherFilmId);
            state.LastResult = answer.Result;
            if (answer.Result == ComparisonResult.Skipped || !scores.TryGetValue(answer.OtherFilmId, out FilmScore? other))
            {
                continue;
            }
            state.Comparisons++;
            bool similar = answer.Result == ComparisonResult.Similar;
            bool inside = other.Score > state.Lower && other.Score < state.Upper;
            if (similar && inside && !previousSimilar || answer.Result == ComparisonResult.Better && other.Score > state.Lower && !IsMinorClash(other, other.Score >= state.Upper ? state.UpperFilmId : null, scores, clashTolerance))
            {
                state.Lower = other.Score;
                state.LowerFilmId = other.FilmId;
            }
            else if (similar && inside && previousSimilar || answer.Result == ComparisonResult.Worse && other.Score < state.Upper && !IsMinorClash(other, other.Score <= state.Lower ? state.LowerFilmId : null, scores, clashTolerance))
            {
                state.Upper = other.Score;
                state.UpperFilmId = other.FilmId;
            }
            previousSimilar = similar;
        }
        return state;
    }

    /// <summary>
    /// Returns whether an answer about one film would only clash with the film setting the opposite bound by a small amount: both shown scores, or raw scores where a film has none, within the tolerance.
    /// </summary>
    /// <param name="other">The film the answer was about.</param>
    /// <param name="clashingFilmId">The film setting the bound it would cross, or null if it wouldn't cross one.</param>
    /// <param name="scores">Every film's score, keyed by film ID.</param>
    /// <param name="clashTolerance">How close, in shown points, two films must be for a clash between them to be ignored.</param>
    private static bool IsMinorClash(FilmScore other, int? clashingFilmId, Dictionary<int, FilmScore> scores, double clashTolerance)
    {
        if (clashingFilmId is not int filmId || !scores.TryGetValue(filmId, out FilmScore? clashing))
        {
            return false;
        }
        double gap = other.DisplayScore is double otherShown && clashing.DisplayScore is double clashingShown ? otherShown - clashingShown : other.Score - clashing.Score;
        return Math.Abs(gap) <= clashTolerance;
    }
}