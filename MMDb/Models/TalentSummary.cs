using System.Text.Json.Serialization;
using MMDb.Helpers;

namespace MMDb.Models;

/// <summary>
/// A director or actor with totals across the films of theirs I have rated.
/// </summary>
public class TalentSummary
{
    /// <summary>
    /// The TMDb person ID.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// The person's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The TMDb profile photo path.
    /// </summary>
    public string? ProfilePath { get; set; }

    /// <summary>
    /// How many of their films I have rated.
    /// </summary>
    public int FilmCount { get; set; }

    /// <summary>
    /// What they did across those films: D for director, A for actor, or D/A for both.
    /// </summary>
    public string Roles { get; set; } = string.Empty;

    /// <summary>
    /// My average rating across their films.
    /// </summary>
    public double AverageMyRating { get; set; }

    /// <summary>
    /// The average community rating across their films that have one.
    /// </summary>
    public double? AverageCommunityRating { get; set; }

    /// <summary>
    /// How many of their films they were billed as a lead in.
    /// </summary>
    public int Leads { get; set; }

    /// <summary>
    /// My average rating, stretched onto the community's 0 to 10 scale, minus the average community rating, if there is one.
    /// </summary>
    public double? Delta => AverageCommunityRating is double communityRating ? Math.Round(RatingScale.FromOneToTen(AverageMyRating) - communityRating, 1) : null;

    /// <summary>
    /// Their films I have rated, used for filtering by title and year.
    /// </summary>
    [JsonIgnore]
    public List<Film> Films { get; set; } = [];

    /// <summary>
    /// The background colour for my average rating.
    /// </summary>
    public string MyRatingColour => RatingColour.ForRating(AverageMyRating);

    /// <summary>
    /// The background colour for the average community rating, if there is one.
    /// </summary>
    public string? CommunityRatingColour => AverageCommunityRating is double rating ? RatingColour.ForRating(rating) : null;
}