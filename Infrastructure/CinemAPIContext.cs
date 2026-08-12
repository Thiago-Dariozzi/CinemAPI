using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure;

public class CinemAPIContext : DbContext
{
    public CinemAPIContext(DbContextOptions<CinemAPIContext> options) : base(options)
    {
    }

    public DbSet<Movie> Movies { get; set; }
    public DbSet<Screen> Screens { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configuraciones y restricciones para la base de datos
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Synopsis).HasMaxLength(5000);
            entity.Property(e => e.DurationMinutes);
            entity.Property(e => e.Genre).HasMaxLength(50);
            entity.Property(e => e.ImageUrl);
            entity.Property(e => e.ReleaseDate);
            entity.Property(e => e.IsActive);
        });
        modelBuilder.Entity<Screen>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Capacity);
            entity.Property(e => e.IsActive);
        });
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MovieId);
            entity.Property(e => e.ScreenId);
            entity.Property(e => e.UserId);
            entity.Property(e => e.BuyDate);
            entity.Property(e => e.FinalPrice);
            entity.Property(e => e.IsActive);
        });
    }
}