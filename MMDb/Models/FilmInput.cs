using System.ComponentModel.DataAnnotations;

namespace MMDb.Models;

/// <summary>
/// Form input for a film's details; the original rating is read-only, since films are scored by ranking.
/// </summary>
public class FilmInput
{
    /// <summary>
    /// The date I watched the film.
    /// </summary>
    [Display(Name = "Watched On")]
    [DataType(DataType.Date)]
    public DateOnly? WatchedOn { get; set; }

    /// <summary>
    /// Creates form input populated from an existing film.
    /// </summary>
    /// <param name="film">The film to copy values from.</param>
    public static FilmInput FromFilm(Film film)
    {
        return new FilmInput { WatchedOn = film.WatchedOn };
    }

    /// <summary>
    /// Copies the form values onto a film.
    /// </summary>
    /// <param name="film">The film to update.</param>
    public void ApplyTo(Film film)
    {
        film.WatchedOn = WatchedOn;
        film.UpdatedAt = DateTime.UtcNow;
    }
}