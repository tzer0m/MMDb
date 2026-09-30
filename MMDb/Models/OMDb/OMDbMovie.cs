using System.Globalization;

namespace MMDb.Models.OMDb;

/// <summary>
/// A movie's ratings from OMDb.
/// </summary>
public class OMDbMovie
{
    /// <summary>
    /// The IMDb ID.
    /// </summary>
    public string? ImdbId { get; set; }

    /// <summary>
    /// The IMDb rating as text, e.g. 8.3, or N/A.
    /// </summary>
    public string? ImdbRating { get; set; }

    /// <summary>
    /// The IMDb vote count as text, e.g. 720,123, or N/A.
    /// </summary>
    public string? ImdbVotes { get; set; }

    /// <summary>
    /// The Metacritic score as text, e.g. 76, or N/A.
    /// </summary>
    public string? Metascore { get; set; }

    /// <summary>
    /// Ratings from each source.
    /// </summary>
    public List<OMDbRating> Ratings { get; set; } = [];

    /// <summary>
    /// True or False, depending on whether the movie was found.
    /// </summary>
    public string? Response { get; set; }

    /// <summary>
    /// The error message when the movie was not found.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Whether the movie was found.
    /// </summary>
    public bool Found => string.Equals(Response, "True", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The IMDb rating out of 10.
    /// </summary>
    public double? ImdbRatingValue => double.TryParse(ImdbRating, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : null;

    /// <summary>
    /// The number of IMDb votes.
    /// </summary>
    public int? ImdbVotesValue => int.TryParse(ImdbVotes, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int value) ? value : null;

    /// <summary>
    /// The Metacritic score out of 100.
    /// </summary>
    public int? MetacriticValue => int.TryParse(Metascore, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : null;

    /// <summary>
    /// The Rotten Tomatoes score as a percentage.
    /// </summary>
    public int? RottenTomatoesValue => int.TryParse(Ratings.FirstOrDefault(x => x.Source == "Rotten Tomatoes")?.Value.TrimEnd('%'), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : null;
}