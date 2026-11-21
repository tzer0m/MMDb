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
        return Contains(person.Name, Name) && (string.IsNullOrWhiteSpace(Film) || person.Films.Any(x => Contains(x.Title, Film))) && (string.IsNullOrWhiteSpace(Role) || person.Roles.Contains(Role, StringComparison.OrdinalIgnoreCase)) && InRange(person.FilmCount, MinFilms, MaxFilms) && InRange(person.AverageMyRating, MinMr, MaxMr) && InRange(person.AverageCommunityRating, MinCr, MaxCr) && InRange(person.Delta, MinDelta, MaxDelta) && InRange(person.Leads, MinLeads, MaxLeads) && (MinYear is null && MaxYear is null || person.Films.Any(x => InRange(x.Year, MinYear, MaxYear)));
    }

    /// <summary>
    /// Returns whether text contains a search, ignoring case, accents and punctuation, or true if there is no search.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="search">The text to look for.</param>
    private static bool Contains(string text, string? search)
    {
        return string.IsNullOrWhiteSpace(search) || SearchText.Simplify(text).Contains(SearchText.Simplify(search), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns whether a value is within a range, or true if the range has no bounds; a missing value fails any bound.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The lowest allowed value.</param>
    /// <param name="max">The highest allowed value.</param>
    private static bool InRange(double? value, double? min, double? max)
    {
        return min is null && max is null || value is double number && (min is null || number >= min) && (max is null || number <= max);
    }
}