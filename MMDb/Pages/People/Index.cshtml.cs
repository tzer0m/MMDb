using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MMDb.Data;
using MMDb.Models;
using MMDb.Options;

namespace MMDb.Pages.People;

/// <summary>
/// Lists every director and actor in my rated films, with totals across those films, filtered and a page at a time.
/// </summary>
/// <param name="db">The database context.</param>
/// <param name="options">The people options.</param>
public class IndexModel(MMDbContext db, IOptions<PeopleOptions> options) : PageModel
{
    /// <summary>
    /// The most rows a single page request can return.
    /// </summary>
    private const int MaxPageLength = 500;

    /// <summary>
    /// The filters from the query string, used to fill in the form.
    /// </summary>
    public TalentFilter Filter { get; set; } = new();

    /// <summary>
    /// How high an actor must be billed for a film to count as a lead role.
    /// </summary>
    public int LeadCount => options.Value.LeadCount;

    /// <summary>
    /// Shows the page with the form filled in from the query string; the rows are loaded separately.
    /// </summary>
    /// <param name="filter">The filters from the query string.</param>
    public void OnGet([FromQuery] TalentFilter filter)
    {
        Filter = filter;
    }

    /// <summary>
    /// Returns one page of talent for the table, filtered and sorted from the DataTables request.
    /// </summary>
    /// <param name="filter">The filters from the form.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<JsonResult> OnGetDataAsync([FromQuery] TalentFilter filter, CancellationToken cancellationToken)
    {
        List<TalentSummary> talent = await LoadTalentAsync(cancellationToken);
        int draw = int.TryParse(Request.Query["draw"].ToString(), out int parsedDraw) ? parsedDraw : 0;
        int start = int.TryParse(Request.Query["start"].ToString(), out int parsedStart) ? Math.Max(0, parsedStart) : 0;
        int length = int.TryParse(Request.Query["length"].ToString(), out int parsedLength) && parsedLength is > 0 and <= MaxPageLength ? parsedLength : MaxPageLength;
        List<TalentSummary> matches = [.. Sort(talent.Where(filter.Matches))];
        return new JsonResult(new TalentPage { Draw = draw, RecordsTotal = talent.Count, RecordsFiltered = matches.Count, Data = [.. matches.Skip(start).Take(length)] });
    }

    /// <summary>
    /// Loads every credit as lightweight rows and totals them per person.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<List<TalentSummary>> LoadTalentAsync(CancellationToken cancellationToken)
    {
        Dictionary<int, Film> films = await db.Films.AsNoTracking().Select(x => new Film { FilmId = x.FilmId, Title = x.Title, Year = x.Year, Rating = x.Rating, TMDbRating = x.TMDbRating, IMDbRating = x.IMDbRating, RottenTomatoes = x.RottenTomatoes, Metacritic = x.Metacritic }).ToDictionaryAsync(x => x.FilmId, cancellationToken);
        List<FilmCredit> credits = await db.FilmCredits.AsNoTracking().Select(x => new FilmCredit { FilmId = x.FilmId, PersonId = x.PersonId, Role = x.Role, Order = x.Order, Person = new Person { PersonId = x.PersonId, Name = x.Person.Name, ProfilePath = x.Person.ProfilePath } }).ToListAsync(cancellationToken);
        return [.. credits.GroupBy(x => x.PersonId).Select(x => ToSummary(x, films))];
    }

    /// <summary>
    /// Sorts talent by the columns in the DataTables request, matched by their data name so columns can be reordered, falling back to most films first, then by name.
    /// </summary>
    /// <param name="talent">The talent to sort.</param>
    private IEnumerable<TalentSummary> Sort(IEnumerable<TalentSummary> talent)
    {
        IOrderedEnumerable<TalentSummary>? sorted = null;
        for (int i = 0; int.TryParse(Request.Query[$"order[{i}][column]"].ToString(), out int column); i++)
        {
            bool descending = Request.Query[$"order[{i}][dir]"].ToString() == "desc";
            Func<TalentSummary, object> key = Request.Query[$"columns[{column}][data]"].ToString() switch
            {
                "name" => x => x.Name,
                "roles" => x => x.Roles,
                "leads" => x => x.Leads,
                "averageMyRating" => x => x.AverageMyRating,
                "averageCommunityRating" => x => x.AverageCommunityRating ?? double.MinValue,
                "delta" => x => x.Delta ?? double.MinValue,
                _ => x => x.FilmCount
            };
            sorted = sorted is null ? descending ? talent.OrderByDescending(key) : talent.OrderBy(key) : descending ? sorted.ThenByDescending(key) : sorted.ThenBy(key);
        }
        return sorted?.ThenBy(x => x.Name) ?? talent.OrderByDescending(x => x.FilmCount).ThenBy(x => x.Name);
    }

    /// <summary>
    /// Totals one person's credits into a summary row.
    /// </summary>
    /// <param name="credits">The person's credits.</param>
    /// <param name="films">Every rated film, keyed by film ID.</param>
    private TalentSummary ToSummary(IGrouping<int, FilmCredit> credits, Dictionary<int, Film> films)
    {
        List<Film> personFilms = [.. credits.Select(x => x.FilmId).Distinct().Select(x => films[x])];
        List<double> communityRatings = [.. personFilms.Where(x => x.CommunityRating is not null).Select(x => x.CommunityRating!.Value)];
        Person person = credits.First().Person;
        int leads = credits.Where(x => x.Role == CreditRole.Cast && x.Order < options.Value.LeadCount).Select(x => x.FilmId).Distinct().Count();
        return new TalentSummary { PersonId = person.PersonId, Name = person.Name, ProfilePath = person.ProfilePath, FilmCount = personFilms.Count, Roles = string.Join("/", credits.Select(x => x.Role).Distinct().Order().Select(x => x == CreditRole.Director ? "D" : "A")), AverageMyRating = Math.Round(personFilms.Average(x => x.Rating), 1), AverageCommunityRating = communityRatings.Count == 0 ? null : Math.Round(communityRatings.Average(), 1), Leads = leads, Films = personFilms };
    }
}