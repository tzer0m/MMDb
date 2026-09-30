using Microsoft.EntityFrameworkCore;
using MMDb.Models;

namespace MMDb.Data;

/// <summary>
/// Database context for MMDb.
/// </summary>
/// <param name="options">The database context options.</param>
public class MMDbContext(DbContextOptions<MMDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The rated films.
    /// </summary>
    public DbSet<Film> Films => Set<Film>();

    /// <summary>
    /// Directors and actors.
    /// </summary>
    public DbSet<Person> People => Set<Person>();

    /// <summary>
    /// Links between films and people.
    /// </summary>
    public DbSet<FilmCredit> FilmCredits => Set<FilmCredit>();

    /// <summary>
    /// Configures the entity model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Film>().Property(x => x.Title).HasMaxLength(300);
        modelBuilder.Entity<Film>().Property(x => x.Director).HasMaxLength(200);
        modelBuilder.Entity<Film>().ToTable(x => x.HasCheckConstraint("CK_Films_Rating", "\"Rating\" BETWEEN 1 AND 10"));
        modelBuilder.Entity<Film>().HasIndex(x => x.Title);
        modelBuilder.Entity<Film>().HasIndex(x => x.TMDbId).IsUnique();
        modelBuilder.Entity<Film>().HasIndex(x => x.IMDbId);
        modelBuilder.Entity<Film>().Property(x => x.IMDbId).HasMaxLength(20);
        modelBuilder.Entity<Person>().Property(x => x.PersonId).ValueGeneratedNever();
        modelBuilder.Entity<Person>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<FilmCredit>().HasKey(x => new { x.FilmId, x.PersonId, x.Role });
        modelBuilder.Entity<FilmCredit>().HasOne(x => x.Film).WithMany(x => x.Credits).HasForeignKey(x => x.FilmId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FilmCredit>().HasOne(x => x.Person).WithMany(x => x.Credits).HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FilmCredit>().HasIndex(x => new { x.PersonId, x.Role });
    }
}