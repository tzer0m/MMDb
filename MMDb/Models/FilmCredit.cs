namespace MMDb.Models;

/// <summary>
/// Links a person to a film in a given role.
/// </summary>
public class FilmCredit
{
    /// <summary>
    /// The ID of the film.
    /// </summary>
    public int FilmId { get; set; }

    /// <summary>
    /// The film.
    /// </summary>
    public Film Film { get; set; } = null!;

    /// <summary>
    /// The TMDb ID of the person.
    /// </summary>
    public int PersonId { get; set; }

    /// <summary>
    /// The person.
    /// </summary>
    public Person Person { get; set; } = null!;

    /// <summary>
    /// The person's role on the film.
    /// </summary>
    public CreditRole Role { get; set; }

    /// <summary>
    /// The billing order, lowest first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// The character played, for cast credits.
    /// </summary>
    public string? Character { get; set; }
}