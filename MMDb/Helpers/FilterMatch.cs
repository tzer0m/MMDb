namespace MMDb.Helpers;

/// <summary>
/// Shared checks for the talent and film filters, where an empty filter always matches.
/// </summary>
public static class FilterMatch
{
    /// <summary>
    /// Returns whether text contains a search, ignoring case, accents and punctuation, or true if there is no search.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="search">The text to look for.</param>
    public static bool Contains(string? text, string? search)
    {
        return string.IsNullOrWhiteSpace(search) || text is not null && SearchText.Simplify(text).Contains(SearchText.Simplify(search), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns whether a number is within a range, or true if the range has no bounds; a missing value fails any bound.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The lowest allowed value.</param>
    /// <param name="max">The highest allowed value.</param>
    public static bool InRange(double? value, double? min, double? max)
    {
        return min is null && max is null || value is double number && (min is null || number >= min) && (max is null || number <= max);
    }

    /// <summary>
    /// Returns whether a date is within a range, or true if the range has no bounds; a missing date fails any bound.
    /// </summary>
    /// <param name="value">The date to check.</param>
    /// <param name="from">The earliest allowed date.</param>
    /// <param name="to">The latest allowed date.</param>
    public static bool InRange(DateOnly? value, DateOnly? from, DateOnly? to)
    {
        return from is null && to is null || value is DateOnly date && (from is null || date >= from) && (to is null || date <= to);
    }
}