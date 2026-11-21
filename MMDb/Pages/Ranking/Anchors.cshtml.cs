using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;
using MMDb.Services;

namespace MMDb.Pages.Ranking;

/// <summary>
/// Picks the anchor films that are ranked first in each band, so other films have something to be compared with.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="ranking">The ranking service.</param>
public class AnchorsModel(MMDbContext db, RankingService ranking) : PageModel
{
    /// <summary>
    /// Every film's score grouped by its original rating, highest first, with films in title order; linked films are left out.
    /// </summary>
    public List<IGrouping<int, FilmScore>> Bands { get; set; } = [];

    /// <summary>
    /// Loads every film's score and whether it is an anchor.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await ranking.EnsureScoresAsync(cancellationToken);
        List<FilmScore> scores = await db.FilmScores.AsNoTracking().Include(x => x.Film).Where(x => x.LinkedToFilmId == null).ToListAsync(cancellationToken);
        Bands = [.. scores.OrderBy(x => x.Film.Title).GroupBy(x => x.Film.Rating).OrderByDescending(x => x.Key)];
    }

    /// <summary>
    /// Saves the ticked films as the anchors, then goes to ranking.
    /// </summary>
    /// <param name="anchorIds">The ticked films.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnPostAsync(List<int> anchorIds, CancellationToken cancellationToken)
    {
        await ranking.SetAnchorsAsync(anchorIds, cancellationToken);
        TempData["Message"] = $"Saved {anchorIds.Count} anchors.";
        return RedirectToPage("/Ranking/Index");
    }
}