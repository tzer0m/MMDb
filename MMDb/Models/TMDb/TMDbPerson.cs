using System.Globalization;

namespace MMDb.Models.TMDb;

/// <summary>
/// A person's details from TMDb.
/// </summary>
public class TMDbPerson
{
    /// <summary>
    /// The TMDb person ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The person's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The person's biography.
    /// </summary>
    public string? Biography { get; set; }

    /// <summary>
    /// The date of birth as yyyy-MM-dd, or null if unknown.
    /// </summary>
    public string? Birthday { get; set; }

    /// <summary>
    /// The date of death as yyyy-MM-dd, or null if still alive or unknown.
    /// </summary>
    public string? Deathday { get; set; }

    /// <summary>
    /// Where the person was born.
    /// </summary>
    public string? PlaceOfBirth { get; set; }

    /// <summary>
    /// The profile photo path.
    /// </summary>
    public string? ProfilePath { get; set; }

    /// <summary>
    /// Parses a TMDb yyyy-MM-dd date, returning null if it is missing or invalid.
    /// </summary>
    /// <param name="value">The date text.</param>
    public static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date) ? date : null;
    }
}