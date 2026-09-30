using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Options;
using MMDb.Services;

namespace MMDb.Pages.People;

/// <summary>
/// Shows a director or actor and every film of theirs I have rated.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="filmData">The film data service.</param>
/// <param name="options">The people options.</param>
/// <param name="logger">The logger.</param>
public partial class DetailsModel(MMDbContext db, FilmDataService filmData, IOptions<PeopleOptions> options, ILogger<DetailsModel> logger) : PageModel
{
    /// <summary>
    /// The person being displayed.
    /// </summary>
    public Person Person { get; set; } = null!;

    /// <summary>
    /// Their films I have rated, oldest first.
    /// </summary>
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// Their role on each film, keyed by film ID: Director, the character they played, or both.
    /// </summary>
    public Dictionary<int, string> Roles { get; set; } = [];

    /// <summary>
    /// My average rating across their films.
    /// </summary>
    public double? AverageMyRating => Films.Count == 0 ? null : Films.Average(x => x.Rating);

    /// <summary>
    /// The average community rating across their films that have one.
    /// </summary>
    public double? AverageCommunityRating => Films.Any(x => x.CommunityRating is not null) ? Films.Where(x => x.CommunityRating is not null).Average(x => x.CommunityRating!.Value) : null;

    /// <summary>
    /// Their birth and death dates with age, and birthplace, e.g. 3 March 1965 (61) · London, England.
    /// </summary>
    public string BirthLine
    {
        get
        {
            List<string> parts = [];
            if (Person.Birthday is DateOnly birthday)
            {
                parts.Add(Person.Deathday is DateOnly deathday ? $"{birthday:d MMMM yyyy} \u2013 {deathday:d MMMM yyyy} ({Age})" : $"{birthday:d MMMM yyyy} ({Age})");
            }
            if (!string.IsNullOrWhiteSpace(Person.PlaceOfBirth))
            {
                parts.Add(Person.PlaceOfBirth);
            }
            return string.Join(" \u00b7 ", parts);
        }
    }

    /// <summary>
    /// The first two paragraphs of their biography.
    /// </summary>
    public string? BiographySummary => Person.Biography is string biography ? string.Join("\n\n", biography.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(2)) : null;

    /// <summary>
    /// Their age now, or at death if they have died.
    /// </summary>
    public int? Age
    {
        get
        {
            if (Person.Birthday is not DateOnly birthday)
            {
                return null;
            }
            DateOnly end = Person.Deathday ?? DateOnly.FromDateTime(DateTime.Today);
            int age = end.Year - birthday.Year;
            return end < birthday.AddYears(age) ? age - 1 : age;
        }
    }

    /// <summary>
    /// Loads the person, refreshing their details from TMDb if stale, and their rated films.
    /// </summary>
    /// <param name="id">The TMDb person ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Person? person = await db.People.FirstOrDefaultAsync(x => x.PersonId == id, cancellationToken);
        if (person is null)
        {
            return NotFound();
        }
        if (person.DetailsUpdatedAt is null || person.DetailsUpdatedAt < DateTime.UtcNow - options.Value.DetailsMaxAge)
        {
            try
            {
                if (await filmData.ApplyPersonDetailsAsync(person, cancellationToken))
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (HttpRequestException ex)
            {
                LogDetailsFailed(logger, ex, person.Name);
            }
        }
        Person = person;
        List<FilmCredit> credits = await db.FilmCredits.AsNoTracking().Include(x => x.Film).Where(x => x.PersonId == id).ToListAsync(cancellationToken);
        Films = [.. credits.Select(x => x.Film).DistinctBy(x => x.FilmId).OrderBy(x => x.Year ?? int.MaxValue).ThenBy(x => x.Title)];
        Roles = credits.GroupBy(x => x.FilmId).ToDictionary(x => x.Key, x => string.Join(", ", x.OrderBy(c => c.Role).Select(c => c.Role == CreditRole.Director ? "Director" : c.Character ?? "Cast")));
        return Page();
    }

    /// <summary>
    /// Logs that a person's details could not be fetched from TMDb.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The error.</param>
    /// <param name="name">The person's name.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not fetch TMDb details for {Name}; showing cached details.")]
    private static partial void LogDetailsFailed(ILogger logger, Exception exception, string name);
}