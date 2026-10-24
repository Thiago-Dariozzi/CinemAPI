using Domain.Interfaces;

namespace CinemAPI.Tests.Fakes;

// Imita a los repositorios reales: las lecturas solo ven entidades activas (filtro global) y
// GetById devuelve la misma instancia guardada, como una entidad trackeada por EF.
public abstract class InMemoryRepository<T> : IBaseRepository<T> where T : class, IEntity
{
    private readonly List<T> _items = [];

    // Incluye las inactivas, para poder verificar el soft delete.
    public IReadOnlyList<T> Stored => _items;

    public void Seed(params T[] entities) => _items.AddRange(entities);

    public Task<IEnumerable<T>> GetAll() => Task.FromResult<IEnumerable<T>>(Active().ToList());

    public Task<T?> GetById(Guid id) => Task.FromResult(Active().FirstOrDefault(e => e.Id == id));

    public void Add(T entity) => _items.Add(entity);

    protected IEnumerable<T> Active() => _items.Where(e => e.IsActive);
}
