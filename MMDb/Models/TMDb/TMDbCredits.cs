namespace MMDb.Models.TMDb;

/// <summary>
/// The cast and crew of a TMDb movie.
/// </summary>
public class TMDbCredits
{
    /// <summary>
    /// The cast, in billing order.
    /// </summary>
    public List<TMDbCastMember> Cast { get; set; } = [];

    /// <summary>
    /// The crew.
    /// </summary>
    public List<TMDbCrewMember> Crew { get; set; } = [];
}