namespace MMDb.Models;

/// <summary>
/// One page of talent for the talent table, in the shape DataTables expects.
/// </summary>
public class TalentPage
{
    /// <summary>
    /// The request counter DataTables sent, echoed back so it can ignore stale responses.
    /// </summary>
    public int Draw { get; set; }

    /// <summary>
    /// How many people there are before filtering.
    /// </summary>
    public int RecordsTotal { get; set; }

    /// <summary>
    /// How many people match the current filters.
    /// </summary>
    public int RecordsFiltered { get; set; }

    /// <summary>
    /// The people on this page.
    /// </summary>
    public List<TalentSummary> Data { get; set; } = [];
}