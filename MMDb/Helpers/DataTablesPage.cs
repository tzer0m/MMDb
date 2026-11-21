namespace MMDb.Helpers;

/// <summary>
/// One page of rows for a server-side DataTable, in the shape DataTables expects.
/// </summary>
public class DataTablesPage<T>
{
    /// <summary>
    /// The request counter DataTables sent, echoed back so it can ignore stale responses.
    /// </summary>
    public int Draw { get; set; }

    /// <summary>
    /// How many rows there are before filtering.
    /// </summary>
    public int RecordsTotal { get; set; }

    /// <summary>
    /// How many rows match the current filters.
    /// </summary>
    public int RecordsFiltered { get; set; }

    /// <summary>
    /// The rows on this page.
    /// </summary>
    public List<T> Data { get; set; } = [];
}