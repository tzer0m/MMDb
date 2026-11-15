namespace MMDb.Models.TMDb;

/// <summary>
/// The cast of a TMDb TV show across every season.
/// </summary>
public class TMDbAggregateCredits
{
    /// <summary>
    /// The cast, in billing order.
    /// </summary>
    public List<TMDbAggregateCastMember> Cast { get; set; } = [];
}