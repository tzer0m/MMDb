using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Services;

namespace MMDb.Pages.Ranking;

/// <summary>
/// Ranks films by asking which of two is better, one question after another.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="ranking">The ranking service.</param>
public class IndexModel(MMDbContext db, RankingService ranking) : PageModel
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
    /// How many films have a settled score, not counting linked films.
    /// </summary>
    public int Settled { get; set; }

    /// <summary>
    /// How many films have a score, settled or not, not counting linked films.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Finds the next question, for the given film if it is still waiting to be ranked, otherwise for the next film in the queue.
    /// </summary>
    /// <param name="filmId">The film to rank, if one was chosen.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnGetAsync(int? filmId, CancellationToken cancellationToken)
    {
        if (TempData["Message"] is string message)
        {
            Messages.Add(message);
        }
        await ranking.EnsureScoresAsync(cancellationToken);
        int? nextId = filmId;
        for (int step = 0; step < MaxSteps; step++)
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
        Total = await db.FilmScores.CountAsync(x => x.LinkedToFilmId == null, cancellationToken);
        Settled = await db.FilmScores.CountAsync(x => x.IsSettled && x.LinkedToFilmId == null, cancellationToken);
    }

    /// <summary>
    /// Records an answer, then carries on with the same film.
    /// </summary>
    /// <param name="filmId">The film being ranked.</param>
    /// <param name="otherFilmId">The film it was compared with.</param>
    /// <param name="result">My answer.</param>
    /// <param name="isCheck">Whether the question was double-checking a big move.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostAnswerAsync(int filmId, int otherFilmId, ComparisonResult result, bool isCheck, CancellationToken cancellationToken)
    {
        bool saved = await ranking.AnswerAsync(filmId, otherFilmId, result, isCheck, cancellationToken);
        return RedirectToPage(new { filmId = saved ? filmId : (int?)null });
    }

    /// <summary>
    /// Takes back the most recent answer and returns to that film.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostUndoAsync(CancellationToken cancellationToken)
    {
        int? filmId = await ranking.UndoAsync(cancellationToken);
        if (filmId is null)
        {
            TempData["Message"] = "There's nothing to undo.";
        }
        return RedirectToPage(new { filmId });
    }
}