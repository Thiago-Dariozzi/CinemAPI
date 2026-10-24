using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// El soft delete lo resuelve el filtro global (HasQueryFilter) de cada configuración, así que
// estas consultas ya excluyen las entidades inactivas. Guardar es responsabilidad de IUnitOfWork.
public abstract class BaseRepository<T>(CinemAPIContext context) : IBaseRepository<T> where T : class, IEntity
{
    protected CinemAPIContext Context { get; } = context;

    public virtual async Task<IEnumerable<T>> GetAll() =>
        await Context.Set<T>().AsNoTracking().ToListAsync();

    // Sin FindAsync: no aplica el filtro global si la entidad ya está trackeada.
    public virtual Task<T?> GetById(Guid id) =>
        Context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);

    public void Add(T entity) => Context.Set<T>().Add(entity);
}
