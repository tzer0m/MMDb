namespace MMDb.Models;

/// <summary>
/// How many films were watched in a week.
/// </summary>
/// <param name="Week">The Monday the week starts on.</param>
/// <param name="Count">The number of films watched.</param>
public record WeeklyCount(DateOnly Week, int Count);