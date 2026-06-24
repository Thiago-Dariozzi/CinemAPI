using System;

namespace Domain.Inerfaces;

public interface IBaseRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAll();
    Task<T?> GetById(Guid id);
    Task<T> Add(T entity);
    Task Update(T entity);
    Task Delete(Guid id);
}