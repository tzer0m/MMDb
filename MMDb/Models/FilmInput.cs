using System.ComponentModel.DataAnnotations;

namespace MMDb.Models;

/// <summary>
/// Form input for rating a film.
/// </summary>
public class FilmInput
{
    /// <summary>
    /// My rating, from 1 to 10.
    /// </summary>
    [Required(ErrorMessage = "Choose a rating.")]
    [Range(1, 10)]
    public int? Rating { get; set; }

    /// <summary>
    /// The date I watched the film.
    /// </summary>
    [Display(Name = "Watched on")]
    [DataType(DataType.Date)]
    public DateOnly? WatchedOn { get; set; }

    /// <summary>
    /// Creates form input populated from an existing film.
    /// </summary>
    /// <param name="film">The film to copy values from.</param>
    public static FilmInput FromFilm(Film film)
    {
        return new FilmInput { Rating = film.Rating, WatchedOn = film.WatchedOn };
    }

    /// <summary>
    /// Copies the form values onto a film.
    /// </summary>
    /// <param name="film">The film to update.</param>
    public void ApplyTo(Film film)
    {
        film.Rating = Rating ?? film.Rating;
        film.WatchedOn = WatchedOn;
        film.UpdatedAt = DateTime.UtcNow;
    }
}