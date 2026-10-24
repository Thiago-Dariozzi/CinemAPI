using Domain.Entities;

namespace Application.Interfaces;

public interface IGenreService
{
    Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken ct);
    Task<Genre> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Genre> AddAsync(Genre genre, CancellationToken ct);
    Task UpdateAsync(Genre genre, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
