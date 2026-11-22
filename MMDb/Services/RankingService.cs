using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Keeps films' scores, which come from comparing films with each other; shown scores are frozen, so placing one film never moves the others.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="options">The ranking options.</param>
/// <param name="logger">The logger.</param>
public partial class RankingService(MMDbContext db, IOptions<RankingOptions> options, ILogger<RankingService> logger)
{
    /// <summary>
    /// Gives every film without a score an unsettled one so it can be ranked; it starts in the middle, since where it starts doesn't affect the questions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task EnsureScoresAsync(CancellationToken cancellationToken = default)
    {
        List<int> filmIds = await db.Films.Where(x => x.FilmScore == null).Select(x => x.FilmId).ToListAsync(cancellationToken);
        if (filmIds.Count == 0)
        {
            return;
        }
        DateTime now = DateTime.UtcNow;
        foreach (int filmId in filmIds)
        {
            db.FilmScores.Add(new FilmScore { FilmId = filmId, Score = 50, ResetAt = now, UpdatedAt = now });
        }
        await db.SaveChangesAsync(cancellationToken);
        LogSeeded(logger, filmIds.Count);
    }

    /// <summary>
    /// Picks the next film to rank: films part way through first, then a random unsettled film; null if there is nothing to rank or nothing settled to compare with.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<int?> PickNextFilmAsync(CancellationToken cancellationToken = default)
    {
        List<int> unsettled = await db.FilmScores.Where(x => !x.IsSettled && x.LinkedToFilmId == null).Select(x => x.FilmId).ToListAsync(cancellationToken);
        if (unsettled.Count == 0 || !await db.FilmScores.AnyAsync(x => x.IsSettled, cancellationToken))
        {
            return null;
        }
        int? inProgress = await db.FilmComparisons.Where(x => !x.Film.FilmScore!.IsSettled && x.Film.FilmScore!.LinkedToFilmId == null && x.CreatedAt >= x.Film.FilmScore!.ResetAt).GroupBy(x => x.FilmId).OrderByDescending(x => x.Count()).Select(x => (int?)x.Key).FirstOrDefaultAsync(cancellationToken);
        return inProgress ?? unsettled[Random.Shared.Next(unsettled.Count)];
    }

    /// <summary>
    /// Gets the next question for an unsettled film, settling it or restarting it first if its answers call for that; null if the film isn't waiting to be ranked, is linked to another film, or nothing is settled to compare it with.
    /// </summary>
    /// <param name="filmId">The film being ranked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<RankingQuestion?> GetQuestionAsync(int filmId, CancellationToken cancellationToken = default)
    {
        Dictionary<int, FilmScore> scores = await db.FilmScores.ToDictionaryAsync(x => x.FilmId, cancellationToken);
        if (!scores.TryGetValue(filmId, out FilmScore? score) || score.IsSettled || score.LinkedToFilmId is not null || !scores.Values.Any(x => x.IsSettled))
        {
            return null;
        }
        Film film = await db.Films.AsNoTracking().FirstAsync(x => x.FilmId == filmId, cancellationToken);
        List<FilmComparison> answers = await db.FilmComparisons.AsNoTracking().Where(x => x.FilmId == filmId && x.CreatedAt >= score.ResetAt).OrderBy(x => x.CreatedAt).ThenBy(x => x.FilmComparisonId).ToListAsync(cancellationToken);
        RankingState state = RankingState.Build(answers, scores, options.Value.ClashTolerance);
        RankingQuestion question = new() { Film = film, Comparisons = state.Comparisons, ComparisonsNeeded = options.Value.MinComparisons };
        if (state.IsContradiction)
        {
            question.Message = await RestartAsync(score, state, scores, film, cancellationToken);
            return question;
        }
        // Only settled films are compared against, so mistakes in unsettled ones don't spread; linked films would just repeat their main film.
        List<FilmScore> pool = [.. scores.Values.Where(x => x.IsSettled && x.LinkedToFilmId is null && x.FilmId != filmId && !state.Asked.Contains(x.FilmId))];
        List<FilmScore> candidates = [.. pool.Where(x => x.Score > state.Lower && x.Score < state.Upper).OrderBy(x => x.Score)];
        bool enoughAnswers = state.Comparisons >= options.Value.MinComparisons;
        if (candidates.Count == 0 || enoughAnswers && state.Upper - state.Lower <= options.Value.SettledRange)
        {
            // With too few answers and nothing left inside the range, check against the nearest films just outside it instead.
            FilmScore? check = candidates.Count == 0 && !enoughAnswers ? pool.OrderBy(x => x.Score <= state.Lower ? state.Lower - x.Score : x.Score - state.Upper).FirstOrDefault() : null;
            if (check is null)
            {
                question.Message = await SettleAsync(score, state, scores, film, cancellationToken);
                question.IsSettled = true;
                return question;
            }
            candidates = [check];
        }
        // The first film is a completely random pick; after that, a similar answer asks about the next film up, and otherwise a random film from the middle third roughly halves the range.
        FilmScore pick = state.Asked.Count == 0 ? candidates[Random.Shared.Next(candidates.Count)] : candidates.Count == 1 || state.LastResult == ComparisonResult.Similar ? candidates[0] : PickFromMiddle(candidates, await GetRecentOpponentsAsync(cancellationToken));
        question.Opponent = await db.Films.AsNoTracking().FirstAsync(x => x.FilmId == pick.FilmId, cancellationToken);
        return question;
    }

    /// <summary>
    /// Records an answer for an unsettled film; returns false if the film isn't waiting to be ranked.
    /// </summary>
    /// <param name="filmId">The film being ranked.</param>
    /// <param name="otherFilmId">The film it was compared with.</param>
    /// <param name="result">My answer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<bool> AnswerAsync(int filmId, int otherFilmId, ComparisonResult result, CancellationToken cancellationToken = default)
    {
        if (filmId == otherFilmId || !await db.FilmScores.AnyAsync(x => x.FilmId == filmId && !x.IsSettled && x.LinkedToFilmId == null, cancellationToken) || !await db.Films.AnyAsync(x => x.FilmId == otherFilmId, cancellationToken))
        {
            return false;
        }
        db.FilmComparisons.Add(new FilmComparison { FilmId = filmId, OtherFilmId = otherFilmId, Result = result, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Removes the most recent answer and makes its film unsettled again; returns that film's ID, or null if there is nothing to undo or the answer was before the film restarted.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<int?> UndoAsync(CancellationToken cancellationToken = default)
    {
        FilmComparison? last = await db.FilmComparisons.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.FilmComparisonId).FirstOrDefaultAsync(cancellationToken);
        FilmScore? score = last is null ? null : await db.FilmScores.FirstOrDefaultAsync(x => x.FilmId == last.FilmId, cancellationToken);
        if (last is null || score is null || last.CreatedAt < score.ResetAt)
        {
            return null;
        }
        db.FilmComparisons.Remove(last);
        score.IsSettled = false;
        score.DisplayScore = null;
        score.UpdatedAt = DateTime.UtcNow;
        await SyncLinkedAsync([score], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return last.FilmId;
    }

    /// <summary>
    /// Sends a film back to be ranked again from scratch, or its main film if it is linked; returns the film to rank, or null if there is no such film.
    /// </summary>
    /// <param name="filmId">The film to re-rank.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<int?> RerankAsync(int filmId, CancellationToken cancellationToken = default)
    {
        await EnsureScoresAsync(cancellationToken);
        FilmScore? score = await db.FilmScores.FirstOrDefaultAsync(x => x.FilmId == filmId, cancellationToken);
        if (score?.LinkedToFilmId is int mainId)
        {
            score = await db.FilmScores.FirstOrDefaultAsync(x => x.FilmId == mainId, cancellationToken);
        }
        if (score is null)
        {
            return null;
        }
        Restart(score, DateTime.UtcNow);
        await SyncLinkedAsync([score], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return score.FilmId;
    }

    /// <summary>
    /// Links a film to another so they share one score, or unlinks it if no film is given; a link to a linked film goes to its main film instead, and films linked to this one move with it.
    /// </summary>
    /// <param name="filmId">The film to link.</param>
    /// <param name="linkedToFilmId">The film to share a score with, or null to unlink.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LinkAsync(int filmId, int? linkedToFilmId, CancellationToken cancellationToken = default)
    {
        await EnsureScoresAsync(cancellationToken);
        Dictionary<int, FilmScore> scores = await db.FilmScores.ToDictionaryAsync(x => x.FilmId, cancellationToken);
        if (!scores.TryGetValue(filmId, out FilmScore? score))
        {
            return;
        }
        int? mainId = linkedToFilmId is int targetId && scores.TryGetValue(targetId, out FilmScore? target) ? target.LinkedToFilmId ?? target.FilmId : null;
        if (mainId == filmId)
        {
            mainId = null;
        }
        if (score.LinkedToFilmId == mainId)
        {
            return;
        }
        DateTime now = DateTime.UtcNow;
        if (mainId is int main)
        {
            foreach (FilmScore follower in scores.Values.Where(x => x.LinkedToFilmId == filmId))
            {
                follower.LinkedToFilmId = main;
                CopyScore(scores[main], follower, now);
            }
            score.LinkedToFilmId = main;
            CopyScore(scores[main], score, now);
        }
        else
        {
            // Unlinked, it has to be ranked on its own.
            score.LinkedToFilmId = null;
            Restart(score, now);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Makes a film unsettled with no shown score and starts its placement again, so earlier answers no longer count.
    /// </summary>
    /// <param name="score">The film's score, tracked.</param>
    /// <param name="now">The current time (UTC).</param>
    private static void Restart(FilmScore score, DateTime now)
    {
        score.IsSettled = false;
        score.DisplayScore = null;
        score.ResetAt = now;
        score.UpdatedAt = now;
    }

    /// <summary>
    /// Copies each main film's scores and whether it is settled onto the films linked to it.
    /// </summary>
    /// <param name="mains">The main films whose scores changed, tracked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task SyncLinkedAsync(List<FilmScore> mains, CancellationToken cancellationToken)
    {
        Dictionary<int, FilmScore> byId = mains.DistinctBy(x => x.FilmId).ToDictionary(x => x.FilmId);
        List<int> mainIds = [.. byId.Keys];
        DateTime now = DateTime.UtcNow;
        foreach (FilmScore follower in await db.FilmScores.Where(x => x.LinkedToFilmId != null && mainIds.Contains(x.LinkedToFilmId.Value)).ToListAsync(cancellationToken))
        {
            CopyScore(byId[follower.LinkedToFilmId!.Value], follower, now);
        }
    }

    /// <summary>
    /// Copies a main film's scores and whether it is settled onto a linked film.
    /// </summary>
    /// <param name="main">The main film's score.</param>
    /// <param name="linked">The linked film's score.</param>
    /// <param name="now">The current time (UTC).</param>
    private static void CopyScore(FilmScore main, FilmScore linked, DateTime now)
    {
        linked.Score = main.Score;
        linked.DisplayScore = main.DisplayScore;
        linked.IsSettled = main.IsSettled;
        linked.SettledAt = main.SettledAt;
        linked.UpdatedAt = now;
    }

    /// <summary>
    /// Picks a random candidate from the middle third, so the search still roughly halves the range each time.
    /// </summary>
    /// <param name="candidates">The candidates, lowest score first; there is at least one.</param>
    /// <param name="recent">Films asked about recently, avoided where there's an alternative.</param>
    private static FilmScore PickFromMiddle(List<FilmScore> candidates, HashSet<int> recent)
    {
        List<FilmScore> middle = [.. candidates.Skip(candidates.Count / 3).Take(candidates.Count - 2 * (candidates.Count / 3))];
        List<FilmScore> fresh = [.. middle.Where(x => !recent.Contains(x.FilmId))];
        List<FilmScore> choices = fresh.Count > 0 ? fresh : middle;
        return choices[Random.Shared.Next(choices.Count)];
    }

    /// <summary>
    /// Gets the films asked about in the most recent questions, across all films.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<HashSet<int>> GetRecentOpponentsAsync(CancellationToken cancellationToken)
    {
        return [.. await db.FilmComparisons.OrderByDescending(x => x.CreatedAt).Take(options.Value.RecentOpponents).Select(x => x.OtherFilmId).ToListAsync(cancellationToken)];
    }

    /// <summary>
    /// Settles a film halfway between the films either side of it, gives it a shown score halfway between theirs, and returns a message saying where it landed.
    /// </summary>
    /// <param name="score">The film's score, tracked.</param>
    /// <param name="state">Its placement state.</param>
    /// <param name="scores">Every film's score, tracked and keyed by film ID.</param>
    /// <param name="film">The film.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> SettleAsync(FilmScore score, RankingState state, Dictionary<int, FilmScore> scores, Film film, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        score.Score = Math.Clamp((state.Lower + state.Upper) / 2, 0.5, 100);
        score.DisplayScore = DisplayScoreBetween(score, scores);
        score.IsSettled = true;
        score.SettledAt = now;
        score.UpdatedAt = now;
        await SyncLinkedAsync([score], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        LogSettled(logger, film.Title, score.Score);
        return $"{film.Title} settled at {score.ShownScore / 10.0:0.0}.";
    }

    /// <summary>
    /// Works out the shown score for a newly settled film: halfway between the shown scores of the settled films either side of it, or the ends of the scale, so no other film moves.
    /// </summary>
    /// <param name="score">The film's score, already placed.</param>
    /// <param name="scores">Every film's score, keyed by film ID.</param>
    private static double DisplayScoreBetween(FilmScore score, Dictionary<int, FilmScore> scores)
    {
        List<FilmScore> settled = [.. scores.Values.Where(x => x.IsSettled && x.LinkedToFilmId is null && x.FilmId != score.FilmId && x.DisplayScore is not null)];
        double below = settled.Where(x => x.Score < score.Score).OrderByDescending(x => x.Score).Select(x => x.DisplayScore!.Value).FirstOrDefault(0);
        double above = settled.Where(x => x.Score > score.Score).OrderBy(x => x.Score).Select(x => x.DisplayScore!.Value).FirstOrDefault(100);
        return (below + above) / 2;
    }

    /// <summary>
    /// Handles answers that clash with the current order: the two films they disagree about become unsettled so they're ranked again, and the film being ranked starts again.
    /// </summary>
    /// <param name="score">The film's score, tracked.</param>
    /// <param name="state">Its placement state.</param>
    /// <param name="scores">Every film's score, tracked and keyed by film ID.</param>
    /// <param name="film">The film being ranked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> RestartAsync(FilmScore score, RankingState state, Dictionary<int, FilmScore> scores, Film film, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        List<int> clashing = [];
        if (state.LowerFilmId is int lowerFilmId)
        {
            clashing.Add(lowerFilmId);
        }
        if (state.UpperFilmId is int upperFilmId)
        {
            clashing.Add(upperFilmId);
        }
        foreach (int filmId in clashing)
        {
            Restart(scores[filmId], now);
        }
        Restart(score, now);
        await SyncLinkedAsync([score, .. clashing.Select(x => scores[x])], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        List<string> titles = await db.Films.Where(x => clashing.Contains(x.FilmId)).Select(x => x.Title).ToListAsync(cancellationToken);
        LogRestarted(logger, film.Title, string.Join(", ", titles));
        return titles.Count == 0 ? $"Those answers contradicted each other, so {film.Title} starts again." : $"Those answers clash with the current order, so {string.Join(" and ", titles)} will be ranked again and {film.Title} starts again.";
    }

    /// <summary>
    /// Logs how many films were given an unsettled score.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of films.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Added unsettled scores for {Count} films.")]
    private static partial void LogSeeded(ILogger logger, int count);

    /// <summary>
    /// Logs that a film's score settled.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="title">The film title.</param>
    /// <param name="score">The raw score it settled at.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "{Title} settled at {Score}.")]
    private static partial void LogSettled(ILogger logger, string title, double score);

    /// <summary>
    /// Logs that answers clashed with the current order, restarting a film.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="title">The film being ranked.</param>
    /// <param name="clashing">The films made unsettled.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Answers for {Title} clashed with {Clashing}; restarting.")]
    private static partial void LogRestarted(ILogger logger, string title, string clashing);
}