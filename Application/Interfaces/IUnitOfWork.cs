namespace Application.Interfaces;

// Un único SaveChanges por caso de uso: si toca varias entidades, se guardan juntas.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Para el caso raro que necesita más de un SaveChanges (ej. el orden que exige un índice único
    // filtrado): se guardan todos o ninguno.
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken);
}
