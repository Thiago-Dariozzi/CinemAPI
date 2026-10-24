using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class CinemAPIContext(DbContextOptions<CinemAPIContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Screen> Screens => Set<Screen>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<Genre> Genres => Set<Genre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CinemAPIContext).Assembly);
    }
}
