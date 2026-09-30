namespace MMDb.Models;

/// <summary>
/// A film's director and top cast, with my other rated films each of them is in.
/// </summary>
public class FilmPeople
{
    /// <summary>
    /// The film's director, if known.
    /// </summary>
    public Person? Director { get; set; }

    /// <summary>
    /// The top five billed cast members.
    /// </summary>
    public List<Person> Cast { get; set; } = [];

    /// <summary>
    /// The character each cast member played, keyed by person ID.
    /// </summary>
    public Dictionary<int, string> Characters { get; set; } = [];

    /// <summary>
    /// My other rated films for each person, keyed by person ID, highest rated first.
    /// </summary>
    public Dictionary<int, List<Film>> OtherFilms { get; set; } = [];
}