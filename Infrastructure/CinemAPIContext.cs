using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class CinemAPIContext : DbContext
{
    public CinemAPIContext(DbContextOptions<CinemAPIContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Screen> Screens => Set<Screen>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Synopsis).HasMaxLength(5000);
            entity.Property(e => e.SuggestedPrice).HasPrecision(10, 2);

            entity.HasOne<Genre>().WithMany().HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Screen>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Collation accent-insensitive + case-insensitive: "Acción", "Accion" y
            // "ACCION" comparan igual también a nivel de base, como última línea de
            // defensa. No cambia cómo se guarda/lee el string, solo cómo se compara.
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50).UseCollation("Modern_Spanish_CI_AI");

            // Filtrado a IsActive = 1: con soft delete, un género dado de baja sigue en
            // la tabla, así que sin el filtro el índice chocaría contra sí mismo apenas
            // se reuse ese nombre.
            entity.HasIndex(e => e.Name).IsUnique().HasFilter("[IsActive] = 1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();   // evita emails duplicados
            entity.Property(e => e.Password).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(30);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FinalPrice).HasPrecision(10, 2);

            entity.HasOne<Movie>().WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Screen>().WithMany().HasForeignKey(e => e.ScreenId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(10, 2);

            entity.HasOne<Movie>().WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Screen>().WithMany().HasForeignKey(e => e.ScreenId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
