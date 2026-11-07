using MMDb.Models;

namespace MMDb.Helpers;

/// <summary>
/// Counts how many films were watched in each week, for the films watched chart.
/// </summary>
public static class WatchedDistribution
{
    /// <summary>
    /// Counts films watched per week from the start date to the latest watched week, including weeks with no films and skipping films without a watched date.
    /// </summary>
    /// <param name="films">The films to count.</param>
    /// <param name="startDate">The earliest watched date to include, or null to include all.</param>
    public static List<WeeklyCount> ForWeeks(IEnumerable<Film> films, DateOnly? startDate)
    {
        List<DateOnly> dates = [.. films.Select(x => x.WatchedOn).OfType<DateOnly>().Where(x => startDate is not DateOnly start || x >= start)];
        if (dates.Count == 0)
        {
            return [];
        }
        Dictionary<DateOnly, int> counts = dates.GroupBy(x => x.AddDays(-(((int)x.DayOfWeek + 6) % 7))).ToDictionary(x => x.Key, x => x.Count());
        List<WeeklyCount> weeks = [];
        DateOnly last = counts.Keys.Max();
        for (DateOnly week = counts.Keys.Min(); week <= last; week = week.AddDays(7))
        {
            weeks.Add(new WeeklyCount(week, counts.GetValueOrDefault(week)));
        }
        return weeks;
    }
}