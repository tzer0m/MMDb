using Microsoft.EntityFrameworkCore;
using MMDb.Data;
using MMDb.Models;

namespace MMDb.Services;

/// <summary>
/// Keeps films' 1 to 100 scores, which come from comparing films with each other rather than from the original 1 to 10 rating.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="logger">The logger.</param>
public partial class RankingService(MMDbContext db, ILogger<RankingService> logger)
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
    /// Logs how many films were given a provisional score.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of films.</param>
    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded provisional scores for {Count} films.")]
    private static partial void LogSeeded(ILogger logger, int count);
}