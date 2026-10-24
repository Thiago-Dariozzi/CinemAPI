namespace Application.Interfaces;

// Un único SaveChanges por caso de uso: si toca varias entidades, se guardan juntas.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
