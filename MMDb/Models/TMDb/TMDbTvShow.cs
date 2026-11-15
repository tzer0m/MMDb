namespace MMDb.Models.TMDb;

/// <summary>
/// Full details of a TV show from TMDb, including credits and external IDs.
/// </summary>
public class TMDbTvShow
{
    /// <summary>
    /// The TMDb TV show ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The show's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The first air date as yyyy-MM-dd, or empty if unknown.
    /// </summary>
    public string? FirstAirDate { get; set; }

    /// <summary>
    /// The last air date as yyyy-MM-dd, or empty if unknown.
    /// </summary>
    public string? LastAirDate { get; set; }

    /// <summary>
    /// Whether the show is still running, e.g. Returning Series, Ended or Canceled.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// A short synopsis of the show.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// The number of seasons.
    /// </summary>
    public int NumberOfSeasons { get; set; }

    /// <summary>
    /// The number of episodes.
    /// </summary>
    public int NumberOfEpisodes { get; set; }

    /// <summary>
    /// The average TMDb user rating, out of 10.
    /// </summary>
    public double VoteAverage { get; set; }

    /// <summary>
    /// The number of TMDb user ratings.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// The poster image path.
    /// </summary>
    public string? PosterPath { get; set; }

    /// <summary>
    /// The external IDs, when requested with append_to_response=external_ids.
    /// </summary>
    public TMDbExternalIds? ExternalIds { get; set; }

    /// <summary>
    /// The cast across every season, when requested with append_to_response=aggregate_credits.
    /// </summary>
    public TMDbAggregateCredits? AggregateCredits { get; set; }

    /// <summary>
    /// The year the show first aired.
    /// </summary>
    public int? Year => FirstAirDate is { Length: >= 4 } && int.TryParse(FirstAirDate[..4], out int year) ? year : null;

    /// <summary>
    /// The year the show last aired.
    /// </summary>
    public int? EndYear => LastAirDate is { Length: >= 4 } && int.TryParse(LastAirDate[..4], out int year) ? year : null;

    /// <summary>
    /// The years the show ran, e.g. 2010–2014, or 2010– if still running.
    /// </summary>
    public string? YearsText
    {
        get
        {
            if (Year is not int year)
            {
                return null;
            }
            if (Status == "Returning Series")
            {
                return $"{year}\u2013";
            }
            return EndYear is int endYear && endYear != year ? $"{year}\u2013{endYear}" : year.ToString();
        }
    }
}