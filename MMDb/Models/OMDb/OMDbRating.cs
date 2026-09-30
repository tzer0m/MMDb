namespace MMDb.Models.OMDb;

/// <summary>
/// A rating from one source, as returned by OMDb.
/// </summary>
public class OMDbRating
{
    /// <summary>
    /// The rating source, e.g. Rotten Tomatoes.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The rating as text, e.g. 92% or 81/100.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}