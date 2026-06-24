using System;
using Domain.Inerfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly CinemAPIContext _context;

    public BaseRepository(CinemAPIContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<T>> GetAll()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<T>? GetById(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task<T?> Add(T entity)
    {
        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task Delete(Guid id)
    {
        var entity = await GetById(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }

    }
    public async Task Update(T entity)
    {
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }
}