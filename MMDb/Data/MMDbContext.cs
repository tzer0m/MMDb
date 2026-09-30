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
    }
}