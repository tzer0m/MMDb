namespace MMDb.Models;

/// <summary>
/// A director or actor, identified by their TMDb person ID.
/// </summary>
public class Person
{
    /// <summary>
    /// The TMDb person ID, used as the primary key.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// The person's name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The TMDb profile photo path.
    /// </summary>
    public string? ProfilePath { get; set; }

    /// <summary>
    /// The films this person is credited on.
    /// </summary>
    public List<FilmCredit> Credits { get; set; } = [];
}