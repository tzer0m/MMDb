namespace MMDb.Helpers;

/// <summary>
/// The paging and sorting parts of a DataTables server-side request, read from the query string.
/// </summary>
public class DataTablesRequest
{
    /// <summary>
    /// The most rows a single request can return.
    /// </summary>
    private const int MaxLength = 500;

    /// <summary>
    /// The request counter, echoed back so DataTables can ignore stale responses.
    /// </summary>
    public int Draw { get; set; }

    /// <summary>
    /// The index of the first row on the page.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// How many rows the page holds.
    /// </summary>
    public int Length { get; set; } = MaxLength;

    /// <summary>
    /// The columns to sort by, in order, as each column's data name and whether it is descending.
    /// </summary>
    public List<(string Data, bool Descending)> Order { get; set; } = [];

    /// <summary>
    /// Reads the request from the query string, capping the page length.
    /// </summary>
    /// <param name="query">The query string.</param>
    public static DataTablesRequest FromQuery(IQueryCollection query)
    {
        DataTablesRequest request = new()
        {
            Draw = int.TryParse(query["draw"].ToString(), out int draw) ? draw : 0,
            Start = int.TryParse(query["start"].ToString(), out int start) ? Math.Max(0, start) : 0,
            Length = int.TryParse(query["length"].ToString(), out int length) && length is > 0 and <= MaxLength ? length : MaxLength
        };
        for (int i = 0; int.TryParse(query[$"order[{i}][column]"].ToString(), out int column); i++)
        {
            request.Order.Add((query[$"columns[{column}][data]"].ToString(), query[$"order[{i}][dir]"].ToString() == "desc"));
        }
        return request;
    }

    /// <summary>
    /// Sorts items by the requested columns, or returns null if no sort was requested.
    /// </summary>
    /// <param name="items">The items to sort.</param>
    /// <param name="keyFor">Picks the sort key for a column's data name.</param>
    public IOrderedEnumerable<T>? Sort<T>(IEnumerable<T> items, Func<string, Func<T, object>> keyFor)
    {
        IOrderedEnumerable<T>? sorted = null;
        foreach ((string data, bool descending) in Order)
        {
            Func<T, object> key = keyFor(data);
            sorted = sorted is null ? descending ? items.OrderByDescending(key) : items.OrderBy(key) : descending ? sorted.ThenByDescending(key) : sorted.ThenBy(key);
        }
        return sorted;
    }

    /// <summary>
    /// Returns the requested page of items.
    /// </summary>
    /// <param name="items">The filtered and sorted items.</param>
    public IEnumerable<T> PageOf<T>(IEnumerable<T> items)
    {
        return items.Skip(Start).Take(Length);
    }

    /// <summary>
    /// Builds the response for a page of rows.
    /// </summary>
    /// <param name="rows">The rows on the page.</param>
    /// <param name="total">How many items there are before filtering.</param>
    /// <param name="filtered">How many items match the filters.</param>
    public DataTablesPage<T> ToPage<T>(List<T> rows, int total, int filtered)
    {
        return new DataTablesPage<T> { Draw = Draw, RecordsTotal = total, RecordsFiltered = filtered, Data = rows };
    }
}