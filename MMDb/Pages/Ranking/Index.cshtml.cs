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
/// <param name="jellyfin">The Jellyfin client.</param>
public class IndexModel(MMDbContext db, RankingService ranking, JellyfinClient jellyfin) : PageModel
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
    /// Whether to go back to the film's page once it settles, rather than carrying on to the next film.
    /// </summary>
    public bool Back { get; set; }

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
    /// <param name="back">Whether to go back to the chosen film's page once it settles.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(int? filmId, bool back, CancellationToken cancellationToken)
    {
        Back = back && filmId is not null;
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
            if (question?.IsSettled == true)
            {
                await PushToJellyfinAsync(id, cancellationToken);
            }
            // A film added or re-ranked from its page goes back there once it's done.
            if (Back && id == filmId && (question is null || question.IsSettled))
            {
                if (Messages.Count > 0)
                {
                    TempData["Message"] = string.Join(" ", Messages);
                }
                return RedirectToPage("/Films/Details", new { id });
            }
            if (question?.Opponent is not null)
            {
                Question = question;
                break;
            }
            // A film that restarted after a clash carries on being ranked; otherwise move to the next film.
            nextId = question is not null && !question.IsSettled ? id : null;
        }
        Total = await db.FilmScores.CountAsync(x => x.LinkedToFilmId == null, cancellationToken);
        Settled = await db.FilmScores.CountAsync(x => x.IsSettled && x.LinkedToFilmId == null, cancellationToken);
        return Page();
    }

    /// <summary>
    /// Records an answer, then carries on with the same film.
    /// </summary>
    /// <param name="filmId">The film being ranked.</param>
    /// <param name="otherFilmId">The film it was compared with.</param>
    /// <param name="result">My answer.</param>
    /// <param name="back">Whether to go back to the film's page once it settles.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostAnswerAsync(int filmId, int otherFilmId, ComparisonResult result, bool back, CancellationToken cancellationToken)
    {
        bool saved = await ranking.AnswerAsync(filmId, otherFilmId, result, cancellationToken);
        return RedirectToPage(new { filmId = saved ? filmId : (int?)null, back = saved && back });
    }

    /// <summary>
    /// Takes back the most recent answer and returns to that film.
    /// </summary>
    /// <param name="back">Whether to go back to the film's page once it settles.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostUndoAsync(bool back, CancellationToken cancellationToken)
    {
        int? filmId = await ranking.UndoAsync(cancellationToken);
        if (filmId is null)
        {
            TempData["Message"] = "There's nothing to undo.";
        }
        return RedirectToPage(new { filmId, back = filmId is not null && back });
    }

    /// <summary>
    /// Sends a film back to be ranked again from scratch, then starts ranking it.
    /// </summary>
    /// <param name="filmId">The film to re-rank.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostRerankAsync(int filmId, CancellationToken cancellationToken)
    {
        int? rankId = await ranking.RerankAsync(filmId, cancellationToken);
        return rankId is int id ? RedirectToPage(new { filmId = id, back = true }) : NotFound();
    }

    /// <summary>
    /// Pushes a newly settled film's score to Jellyfin, noting any failure in the messages.
    /// </summary>
    /// <param name="filmId">The film that settled.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task PushToJellyfinAsync(int filmId, CancellationToken cancellationToken)
    {
        Film? film = await db.Films.AsNoTracking().Include(x => x.FilmScore).FirstOrDefaultAsync(x => x.FilmId == filmId, cancellationToken);
        try
        {
            if (film is not null)
            {
                await jellyfin.PushRatingAsync(film, cancellationToken);
            }
        }
        catch (HttpRequestException ex)
        {
            Messages.Add($"Pushing the score to Jellyfin failed: {ex.Message}");
        }
    }
}