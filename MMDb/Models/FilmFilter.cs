using MMDb.Helpers;

namespace MMDb.Models;

/// <summary>
/// The filters on the films page, bound from the query string; empty values are ignored.
/// </summary>
public class FilmFilter
{
    /// <summary>
    /// Text the title must contain.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Text the director must contain.
    /// </summary>
    public string? Director { get; set; }

    /// <summary>
    /// Text the name of someone in the cast must contain.
    /// </summary>
    public string? Cast { get; set; }

    /// <summary>
    /// watched for my rated films only, or unwatched for the unrated films in my Jellyfin library only.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// The earliest release year.
    /// </summary>
    public int? MinYear { get; set; }

    /// <summary>
    /// The latest release year.
    /// </summary>
    public int? MaxYear { get; set; }

    /// <summary>
    /// The lowest my rating.
    /// </summary>
    public int? MinMr { get; set; }

    /// <summary>
    /// The highest my rating.
    /// </summary>
    public int? MaxMr { get; set; }

    /// <summary>
    /// The lowest community rating.
    /// </summary>
    public double? MinCr { get; set; }

    /// <summary>
    /// The highest community rating.
    /// </summary>
    public double? MaxCr { get; set; }

    /// <summary>
    /// The lowest my rating minus community rating.
    /// </summary>
    public double? MinDelta { get; set; }

    /// <summary>
    /// The highest my rating minus community rating.
    /// </summary>
    public double? MaxDelta { get; set; }

    /// <summary>
    /// Returns whether a film passes every filter that has a value.
    /// </summary>
    /// <param name="film">The film to check; unrated library films have a film ID of 0.</param>
    /// <param name="castFilmIds">The IDs of films whose cast matches, or null if there is no cast filter.</param>
    public bool Matches(Film film, HashSet<int>? castFilmIds)
    {
        bool rated = film.FilmId != 0;
        return FilterMatch.Contains(film.Title, Title) && FilterMatch.Contains(film.Director, Director) && (castFilmIds is null || castFilmIds.Contains(film.FilmId)) && (Status != "watched" || rated) && (Status != "unwatched" || !rated) && FilterMatch.InRange(film.Year, MinYear, MaxYear) && FilterMatch.InRange(rated ? film.Rating : (int?)null, MinMr, MaxMr) && FilterMatch.InRange(film.CommunityRating, MinCr, MaxCr) && FilterMatch.InRange(rated ? film.RatingDelta : null, MinDelta, MaxDelta);
    }
}