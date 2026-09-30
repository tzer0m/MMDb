namespace MMDb.Models.Jellyfin;

/// <summary>
/// A list of items from the Jellyfin items endpoint.
/// </summary>
public class JellyfinItemsResponse
{
    /// <summary>
    /// The matching items.
    /// </summary>
    public List<JellyfinItem> Items { get; set; } = [];

    /// <summary>
    /// The total number of matching items.
    /// </summary>
    public int TotalRecordCount { get; set; }
}