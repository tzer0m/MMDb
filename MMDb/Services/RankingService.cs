using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Options;

namespace MMDb.Services;

/// <summary>
/// Keeps films' 1 to 100 scores, which come from comparing films with each other rather than from the original 1 to 10 rating.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="options">The ranking options.</param>
/// <param name="logger">The logger.</param>
public partial class RankingService(MMDbContext db, IOptions<RankingOptions> options, ILogger<RankingService> logger)
{
    /// <summary>
    /// Gives every film without a score a provisional one from its original rating, spread evenly across that rating's band, e.g. 7s across 61 to 70.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task EnsureScoresAsync(CancellationToken cancellationToken = default)
    {
        List<Film> films = await db.Films.Where(x => x.FilmScore == null).ToListAsync(cancellationToken);
        if (films.Count == 0)
        {
            return;
        }
        DateTime now = DateTime.UtcNow;
        // Within a band, films the community rates higher start higher, as a better first guess than random.
        // Scores stay above the band's lower edge plus a half, so every one still shows as that band once rounded, e.g. 61 to 70 for a 7.
        foreach (IGrouping<int, Film> band in films.GroupBy(x => x.Rating))
        {
            List<Film> ordered = [.. band.OrderBy(x => x.CommunityRating ?? 5).ThenBy(x => x.Title)];
            for (int i = 0; i < ordered.Count; i++)
            {
                double score = (band.Key - 1) * 10 + 0.5 + (i + 0.5) * 9.5 / ordered.Count;
                db.FilmScores.Add(new FilmScore { FilmId = ordered[i].FilmId, Score = score, ResetAt = now, UpdatedAt = now });
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        LogSeeded(logger, films.Count);
    }

    /// <summary>
    /// Picks the next film to rank: unsettled anchors first, then films part way through, then a random unsettled film; null if there is nothing to rank or nothing settled to compare with yet.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<int?> PickNextFilmAsync(CancellationToken cancellationToken = default)
    {
        List<FilmScore> unsettled = await db.FilmScores.AsNoTracking().Where(x => !x.IsSettled && x.LinkedToFilmId == null).ToListAsync(cancellationToken);
        if (unsettled.Count == 0)
        {
            return null;
        }
        if (unsettled.Where(x => x.IsAnchor).OrderByDescending(x => x.Score).FirstOrDefault() is FilmScore anchor)
        {
            return anchor.FilmId;
        }
        if (!await db.FilmScores.AnyAsync(x => x.IsSettled, cancellationToken))
        {
            return null;
        }
        int? inProgress = await db.FilmComparisons.Where(x => !x.Film.FilmScore!.IsSettled && x.Film.FilmScore!.LinkedToFilmId == null && x.CreatedAt >= x.Film.FilmScore!.ResetAt).GroupBy(x => x.FilmId).OrderByDescending(x => x.Count()).Select(x => (int?)x.Key).FirstOrDefaultAsync(cancellationToken);
        return inProgress ?? unsettled[Random.Shared.Next(unsettled.Count)].FilmId;
    }

    /// <summary>
    /// Gets the next question for an unsettled film, settling it or restarting it first if its answers call for that; null if the film isn't waiting to be ranked, is linked to another film, or isn't an anchor and nothing is settled to compare it with.
    /// </summary>
    /// <param name="filmId">The film being ranked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<RankingQuestion?> GetQuestionAsync(int filmId, CancellationToken cancellationToken = default)
    {
        Dictionary<int, FilmScore> scores = await db.FilmScores.ToDictionaryAsync(x => x.FilmId, cancellationToken);
        if (!scores.TryGetValue(filmId, out FilmScore? score) || score.IsSettled || score.LinkedToFilmId is not null || !score.IsAnchor && !scores.Values.Any(x => x.IsSettled))
        {
            return null;
        }
        Film film = await db.Films.AsNoTracking().FirstAsync(x => x.FilmId == filmId, cancellationToken);
        List<FilmComparison> answers = await db.FilmComparisons.AsNoTracking().Where(x => x.FilmId == filmId && x.CreatedAt >= score.ResetAt).OrderBy(x => x.CreatedAt).ThenBy(x => x.FilmComparisonId).ToListAsync(cancellationToken);
        RankingState state = RankingState.Build(score, answers, scores);
        int needed = score.IsAnchor ? 0 : options.Value.MinComparisons;
        RankingQuestion question = new() { Film = film, Comparisons = state.Comparisons, ComparisonsNeeded = needed, IsAnchor = score.IsAnchor };
        if (state.IsContradiction)
        {
            question.Message = await RestartAsync(score, state, scores, film, cancellationToken);
            return question;
        }
        // Only settled films are compared against, so mistakes in unsettled ones don't spread; linked films would just repeat their main film.
        List<FilmScore> candidates = [.. scores.Values.Where(x => x.IsSettled && x.LinkedToFilmId is null && x.FilmId != filmId && x.Score > state.Lower && x.Score < state.Upper && !state.Asked.Contains(x.FilmId)).OrderBy(x => x.Score)];
        bool enoughAnswers = state.Comparisons >= needed;
        if (candidates.Count == 0 || enoughAnswers && state.Upper - state.Lower <= options.Value.SettledRange)
        {
            // With too few answers and nothing left inside the range, check against the nearest films just outside it instead.
            FilmScore? check = candidates.Count == 0 && !enoughAnswers ? scores.Values.Where(x => x.IsSettled && x.LinkedToFilmId is null && x.FilmId != filmId && !state.Asked.Contains(x.FilmId)).OrderBy(x => x.Score <= state.Lower ? state.Lower - x.Score : x.Score - state.Upper).FirstOrDefault() : null;
            if (check is null)
            {
                question.Message = await SettleAsync(score, state, film, cancellationToken);
                return question;
            }
            candidates = [check];
        }
        // After a similar answer, ask about the next film up to decide which side it belongs; otherwise search outwards from its band.
        FilmScore pick = state.LastResult == ComparisonResult.Similar ? candidates[0] : PickFromBand(candidates, score.Band);
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
        score.UpdatedAt = DateTime.UtcNow;
        await SyncLinkedAsync([score], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return last.FilmId;
    }

    /// <summary>
    /// Makes exactly the given films anchors, leaving out linked films; anchors that are already settled stay settled.
    /// </summary>
    /// <param name="filmIds">The films to make anchors.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAnchorsAsync(IEnumerable<int> filmIds, CancellationToken cancellationToken = default)
    {
        HashSet<int> anchors = [.. filmIds];
        DateTime now = DateTime.UtcNow;
        foreach (FilmScore score in await db.FilmScores.ToListAsync(cancellationToken))
        {
            bool isAnchor = anchors.Contains(score.FilmId) && score.LinkedToFilmId is null;
            if (score.IsAnchor != isAnchor)
            {
                score.IsAnchor = isAnchor;
                score.ResetAt = score.IsSettled ? score.ResetAt : now;
                score.UpdatedAt = now;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
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
            score.IsAnchor = false;
            CopyScore(scores[main], score, now);
        }
        else
        {
            // Unlinked, it starts from the shared score but has to be ranked on its own.
            score.LinkedToFilmId = null;
            score.IsSettled = false;
            score.ResetAt = now;
            score.UpdatedAt = now;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Copies each main film's score and whether it is settled onto the films linked to it.
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
    /// Copies a main film's score and whether it is settled onto a linked film.
    /// </summary>
    /// <param name="main">The main film's score.</param>
    /// <param name="linked">The linked film's score.</param>
    /// <param name="now">The current time (UTC).</param>
    private static void CopyScore(FilmScore main, FilmScore linked, DateTime now)
    {
        linked.Score = main.Score;
        linked.IsSettled = main.IsSettled;
        linked.SettledAt = main.SettledAt;
        linked.UpdatedAt = now;
    }

    /// <summary>
    /// Picks the middle candidate within the film's band, widening by a band each side until there are candidates, so the search starts where its original rating suggests.
    /// </summary>
    /// <param name="candidates">The candidates, lowest score first; there is at least one.</param>
    /// <param name="band">The film's band.</param>
    private static FilmScore PickFromBand(List<FilmScore> candidates, int band)
    {
        for (int widen = 0; ; widen++)
        {
            double low = (band - 1 - widen) * 10;
            double high = (band + widen) * 10;
            List<FilmScore> inWindow = [.. candidates.Where(x => x.Score >= low && x.Score <= high)];
            if (inWindow.Count > 0)
            {
                return inWindow[inWindow.Count / 2];
            }
        }
    }

    /// <summary>
    /// Settles a film halfway between the films either side of it and returns a message saying where it landed.
    /// </summary>
    /// <param name="score">The film's score, tracked.</param>
    /// <param name="state">Its placement state.</param>
    /// <param name="film">The film.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<string> SettleAsync(FilmScore score, RankingState state, Film film, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        score.Score = Math.Clamp((state.Lower + state.Upper) / 2, 0.5, 100);
        score.IsSettled = true;
        score.SettledAt = now;
        score.UpdatedAt = now;
        await SyncLinkedAsync([score], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        LogSettled(logger, film.Title, score.Score);
        return $"{film.Title} settled at {score.DisplayScore}.";
    }

    /// <summary>
    /// Handles answers that contradict the current order: the two films they disagree about become unsettled, and the film being ranked starts again.
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
            scores[filmId].IsSettled = false;
            scores[filmId].ResetAt = now;
            scores[filmId].UpdatedAt = now;
        }
        score.ResetAt = now;
        score.UpdatedAt = now;
        await SyncLinkedAsync([score, .. clashing.Select(x => scores[x])], cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        List<string> titles = await db.Films.Where(x => clashing.Contains(x.FilmId)).Select(x => x.Title).ToListAsync(cancellationToken);
        LogRestarted(logger, film.Title, string.Join(", ", titles));
        return titles.Count == 0 ? $"Those answers contradicted each other, so {film.Title} starts again." : $"Those answers clash with the current order, so {string.Join(" and ", titles)} will be ranked again and {film.Title} starts again.";
    }

    /// <summary>
    /// Logs how many films were given a provisional score.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of films.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded provisional scores for {Count} films.")]
    private static partial void LogSeeded(ILogger logger, int count);

    /// <summary>
    /// Logs that a film's score settled.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="title">The film title.</param>
    /// <param name="score">The score it settled at.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "{Title} settled at {Score}.")]
    private static partial void LogSettled(ILogger logger, string title, double score);

    /// <summary>
    /// Logs that answers contradicted the current order, restarting a film.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="title">The film being ranked.</param>
    /// <param name="clashing">The films made unsettled.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Answers for {Title} clashed with {Clashing}; restarting.")]
    private static partial void LogRestarted(ILogger logger, string title, string clashing);
}