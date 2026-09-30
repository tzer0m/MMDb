namespace MMDb.Models.TMDb;

/// <summary>
/// Every film a person acted in or worked on, from TMDb.
/// </summary>
public class TMDbPersonCredits
{
    /// <summary>
    /// The films they acted in.
    /// </summary>
    public List<TMDbPersonCredit> Cast { get; set; } = [];

    /// <summary>
    /// The films they worked on behind the camera.
    /// </summary>
    public List<TMDbPersonCredit> Crew { get; set; } = [];
}