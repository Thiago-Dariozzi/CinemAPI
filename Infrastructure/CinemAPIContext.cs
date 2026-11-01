using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Data.SqlClient;
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
    public DbSet<Fee> Fees => Set<Fee>();
    public DbSet<Purchase> Purchases => Set<Purchase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CinemAPIContext).Assembly);
    }

    // 2601/2627: violación de índice único o de UNIQUE. Application no conoce EF ni SQL Server,
    // así que se traduce acá a una excepción de dominio.
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new UniqueConstraintConflictException(ex);
        }
    }

    // Si work falla, el Dispose sin Commit deshace lo que ya se había guardado.
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        await work(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
