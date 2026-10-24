namespace Domain.Interfaces;

// Las lecturas solo ven entidades activas. GetById devuelve la entidad trackeada: el servicio
// la modifica y guarda con IUnitOfWork, sin Update ni segunda instancia.
public interface IBaseRepository<T> where T : class, IEntity
{
    Task<IEnumerable<T>> GetAll();
    Task<T?> GetById(Guid id);
    void Add(T entity);
}
