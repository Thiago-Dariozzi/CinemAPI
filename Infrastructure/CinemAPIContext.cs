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
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Synopsis).HasMaxLength(5000);
            entity.Property(e => e.Genre).HasMaxLength(50);
        });

        modelBuilder.Entity<Screen>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
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
    }
}