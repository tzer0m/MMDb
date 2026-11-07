namespace MMDb.Options;

/// <summary>
/// Configuration for the charts.
/// </summary>
public class ChartOptions
{
    /// <summary>
    /// The earliest watched date included in the films watched chart, because earlier dates are inaccurate; all dates are included when not set.
    /// </summary>
    public DateOnly? WatchedStartDate { get; set; }
}