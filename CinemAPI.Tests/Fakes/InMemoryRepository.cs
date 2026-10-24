using Domain.Interfaces;

namespace CinemAPI.Tests.Fakes;

// Imita a los repositorios reales: las lecturas solo ven entidades activas (soft delete).
public abstract class InMemoryRepository<T>(Func<T, Guid> getId, Func<T, bool> isActive) : IBaseRepository<T>
    where T : class
{
    private readonly List<T> _items = [];

    // Incluye las inactivas, para poder verificar el soft delete.
    public IReadOnlyList<T> Stored => _items;

    public void Seed(params T[] entities) => _items.AddRange(entities);

    public Task<IEnumerable<T>> GetAll() => Task.FromResult<IEnumerable<T>>(Active().ToList());

    public Task<T?> GetById(Guid id) => Task.FromResult(Active().FirstOrDefault(e => getId(e) == id));

    public Task<T> Add(T entity)
    {
        _items.Add(entity);
        return Task.FromResult(entity);
    }

    public Task Update(T entity)
    {
        var index = _items.FindIndex(e => getId(e) == getId(entity));
        _items[index] = entity;
        return Task.CompletedTask;
    }

    public Task Delete(Guid id)
    {
        _items.RemoveAll(e => getId(e) == id);
        return Task.CompletedTask;
    }

    protected IEnumerable<T> Active() => _items.Where(isActive);
}
