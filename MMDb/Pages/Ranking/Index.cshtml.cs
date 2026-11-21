using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Options;
using MMDb.Services;

namespace MMDb.Pages.Ranking;

/// <summary>
/// Ranks films by asking whether one is better, similar or worse than another, a session of questions at a time.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="ranking">The ranking service.</param>
/// <param name="options">The ranking options.</param>
public class IndexModel(MMDbContext db, RankingService ranking, IOptions<RankingOptions> options) : PageModel
{
    /// <summary>
    /// How many films can settle or restart without needing a question before the page stops looking for one.
    /// </summary>
    private const int MaxSteps = 20;

    /// <summary>
    /// The question to ask, or null if there isn't one.
    /// </summary>
    public RankingQuestion? Question { get; set; }

    /// <summary>
    /// What happened since the last answer, e.g. films that settled.
    /// </summary>
    public List<string> Messages { get; set; } = [];

    /// <summary>
    /// How many questions have been answered this session.
    /// </summary>
    public int Answered { get; set; }

    /// <summary>
    /// How many answers make up one session.
    /// </summary>
    public int SessionLength => options.Value.SessionLength;

    /// <summary>
    /// Whether this session's questions have all been answered.
    /// </summary>
    public bool SessionDone => Answered >= SessionLength;

    /// <summary>
    /// How many films have a settled score.
    /// </summary>
    public int Settled { get; set; }

    /// <summary>
    /// How many films have a score, settled or not.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Finds the next question, for the given film if it is still waiting to be ranked, otherwise for the next film in the queue.
    /// </summary>
    /// <param name="filmId">The film to rank, if one was chosen.</param>
    /// <param name="n">How many questions have been answered this session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnGetAsync(int? filmId, int n, CancellationToken cancellationToken)
    {
        Answered = Math.Max(0, n);
        if (TempData["Message"] is string message)
        {
            Messages.Add(message);
        }
        await ranking.EnsureScoresAsync(cancellationToken);
        int? nextId = filmId;
        for (int step = 0; step < MaxSteps && !SessionDone; step++)
        {
            nextId ??= await ranking.PickNextFilmAsync(cancellationToken);
            if (nextId is not int id)
            {
                break;
            }
            RankingQuestion? question = await ranking.GetQuestionAsync(id, cancellationToken);
            if (question?.Message is string text)
            {
                Messages.Add(text);
            }
            if (question?.Opponent is not null)
            {
                Question = question;
                break;
            }
            nextId = null;
        }
        Total = await db.FilmScores.CountAsync(cancellationToken);
        Settled = await db.FilmScores.CountAsync(x => x.IsSettled, cancellationToken);
    }

    /// <summary>
    /// Records an answer, then carries on with the same film.
    /// </summary>
    /// <param name="filmId">The film being ranked.</param>
    /// <param name="otherFilmId">The film it was compared with.</param>
    /// <param name="result">My answer.</param>
    /// <param name="n">How many questions had been answered this session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostAnswerAsync(int filmId, int otherFilmId, ComparisonResult result, int n, CancellationToken cancellationToken)
    {
        bool saved = await ranking.AnswerAsync(filmId, otherFilmId, result, cancellationToken);
        return RedirectToPage(new { filmId = saved ? filmId : (int?)null, n = saved && result != ComparisonResult.Skipped ? n + 1 : n });
    }

    /// <summary>
    /// Takes back the most recent answer and returns to that film.
    /// </summary>
    /// <param name="n">How many questions had been answered this session.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostUndoAsync(int n, CancellationToken cancellationToken)
    {
        int? filmId = await ranking.UndoAsync(cancellationToken);
        if (filmId is null)
        {
            TempData["Message"] = "There's nothing to undo.";
        }
        return RedirectToPage(new { filmId, n = filmId is null ? n : Math.Max(0, n - 1) });
    }
}