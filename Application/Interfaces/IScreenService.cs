using Domain.Entities;

namespace Application.Interfaces;

public interface IScreenService
{
    Task<IReadOnlyList<Screen>> GetAllAsync(CancellationToken ct);
    Task<Screen> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Screen> AddAsync(Screen screen, CancellationToken ct);
    Task UpdateAsync(Screen screen, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
