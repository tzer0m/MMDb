namespace MMDb.Models.Import;

// ONE-TIME IMPORT: remove this class once the Jellyfin/IMDb import is complete.
/// <summary>
/// A single rating from an IMDb ratings CSV export.
/// </summary>
public class IMDbRatingRow
{
    /// <summary>
    /// The IMDb ID, e.g. tt0113277.
    /// </summary>
    public string IMDbId { get; set; } = string.Empty;

    /// <summary>
    /// My rating, from 1 to 10.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// The date I rated the title.
    /// </summary>
    public DateOnly? DateRated { get; set; }

    /// <summary>
    /// The title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The release year.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The IMDb title type, e.g. Movie or TV Series.
    /// </summary>
    public string TitleType { get; set; } = string.Empty;
}