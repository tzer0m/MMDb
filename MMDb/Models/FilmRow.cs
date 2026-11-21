namespace MMDb.Models;

/// <summary>
/// A film as a row in the films table.
/// </summary>
public class FilmRow
{
    /// <summary>
    /// The link to the film's page, or its preview if it is unrated.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The poster image URL, if there is one.
    /// </summary>
    public string? PosterUrl { get; set; }

    /// <summary>
    /// The film's title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The year the film was released.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The film's director.
    /// </summary>
    public string? Director { get; set; }

    /// <summary>
    /// My rating, or null if it is unrated.
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// The background colour for my rating.
    /// </summary>
    public string? RatingColour { get; set; }

    /// <summary>
    /// The community rating, if there is one.
    /// </summary>
    public double? CommunityRating { get; set; }

    /// <summary>
    /// The background colour for the community rating.
    /// </summary>
    public string? CommunityRatingColour { get; set; }

    /// <summary>
    /// My rating minus the community rating.
    /// </summary>
    public double? Delta { get; set; }

    /// <summary>
    /// The date I watched the film.
    /// </summary>
    public DateOnly? WatchedOn { get; set; }
}