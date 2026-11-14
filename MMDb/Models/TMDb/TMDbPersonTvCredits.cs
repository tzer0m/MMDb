namespace MMDb.Models.TMDb;

/// <summary>
/// Every TV show a person appeared in, from TMDb.
/// </summary>
public class TMDbPersonTvCredits
{
    /// <summary>
    /// The shows they acted in.
    /// </summary>
    public List<TMDbTvCredit> Cast { get; set; } = [];
}