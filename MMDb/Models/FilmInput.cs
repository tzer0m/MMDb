using System.ComponentModel.DataAnnotations;

namespace MMDb.Models;

/// <summary>
/// Form input for adding or editing a film.
/// </summary>
public class FilmInput
{
    /// <summary>
    /// The film's title.
    /// </summary>
    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The year the film was released.
    /// </summary>
    [Range(1888, 2100)]
    public int? Year { get; set; }

    /// <summary>
    /// The film's director.
    /// </summary>
    [StringLength(200)]
    public string? Director { get; set; }

    /// <summary>
    /// The film's runtime in minutes.
    /// </summary>
    [Display(Name = "Runtime (minutes)")]
    [Range(1, 1000)]
    public int? RuntimeMinutes { get; set; }

    /// <summary>
    /// A short synopsis of the film.
    /// </summary>
    public string? Overview { get; set; }

    /// <summary>
    /// My rating, from 1 to 10.
    /// </summary>
    [Range(1, 10)]
    public int Rating { get; set; } = 5;

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
        return new FilmInput { Title = film.Title, Year = film.Year, Director = film.Director, RuntimeMinutes = film.RuntimeMinutes, Overview = film.Overview, Rating = film.Rating, WatchedOn = film.WatchedOn };
    }

    /// <summary>
    /// Copies the form values onto a film.
    /// </summary>
    /// <param name="film">The film to update.</param>
    public void ApplyTo(Film film)
    {
        film.Title = Title.Trim();
        film.Year = Year;
        film.Director = string.IsNullOrWhiteSpace(Director) ? null : Director.Trim();
        film.RuntimeMinutes = RuntimeMinutes;
        film.Overview = string.IsNullOrWhiteSpace(Overview) ? null : Overview.Trim();
        film.Rating = Rating;
        film.WatchedOn = WatchedOn;
        film.UpdatedAt = DateTime.UtcNow;
    }
}