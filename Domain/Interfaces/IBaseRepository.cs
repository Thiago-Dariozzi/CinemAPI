namespace Domain.Interfaces;

// Las lecturas solo ven entidades activas. GetByIdAsync devuelve la entidad trackeada: el servicio
// la modifica y guarda con IUnitOfWork, sin Update ni segunda instancia.
public interface IBaseRepository<T> where T : class, IEntity
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct);
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    void Add(T entity);
}
