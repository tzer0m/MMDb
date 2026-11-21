using MMDb.Helpers;

namespace MMDb.Models;

/// <summary>
/// The filters on the talent page, bound from the query string; empty values are ignored.
/// </summary>
public class TalentFilter
{
    /// <summary>
    /// Text the person's name must contain.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Text the title of at least one of their films must contain.
    /// </summary>
    public string? Film { get; set; }

    /// <summary>
    /// D for directors or A for actors.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The fewest rated films.
    /// </summary>
    public int? MinFilms { get; set; }

    /// <summary>
    /// The most rated films.
    /// </summary>
    public int? MaxFilms { get; set; }

    /// <summary>
    /// The lowest average my rating.
    /// </summary>
    public double? MinMr { get; set; }

    /// <summary>
    /// The highest average my rating.
    /// </summary>
    public double? MaxMr { get; set; }

    /// <summary>
    /// The lowest average community rating.
    /// </summary>
    public double? MinCr { get; set; }

    /// <summary>
    /// The highest average community rating.
    /// </summary>
    public double? MaxCr { get; set; }

    /// <summary>
    /// The lowest average my rating minus community rating.
    /// </summary>
    public double? MinDelta { get; set; }

    /// <summary>
    /// The highest average my rating minus community rating.
    /// </summary>
    public double? MaxDelta { get; set; }

    /// <summary>
    /// The fewest lead roles.
    /// </summary>
    public int? MinLeads { get; set; }

    /// <summary>
    /// The most lead roles.
    /// </summary>
    public int? MaxLeads { get; set; }

    /// <summary>
    /// The earliest release year of at least one of their films.
    /// </summary>
    public int? MinYear { get; set; }

    /// <summary>
    /// The latest release year of at least one of their films.
    /// </summary>
    public int? MaxYear { get; set; }

    /// <summary>
    /// Returns whether a person passes every filter that has a value.
    /// </summary>
    /// <param name="person">The person to check.</param>
    public bool Matches(TalentSummary person)
    {
        return FilterMatch.Contains(person.Name, Name) && (string.IsNullOrWhiteSpace(Film) || person.Films.Any(x => FilterMatch.Contains(x.Title, Film))) && (string.IsNullOrWhiteSpace(Role) || person.Roles.Contains(Role, StringComparison.OrdinalIgnoreCase)) && FilterMatch.InRange(person.FilmCount, MinFilms, MaxFilms) && FilterMatch.InRange(person.AverageMyRating, MinMr, MaxMr) && FilterMatch.InRange(person.AverageCommunityRating, MinCr, MaxCr) && FilterMatch.InRange(person.Delta, MinDelta, MaxDelta) && FilterMatch.InRange(person.Leads, MinLeads, MaxLeads) && (MinYear is null && MaxYear is null || person.Films.Any(x => FilterMatch.InRange(x.Year, MinYear, MaxYear)));
    }
}