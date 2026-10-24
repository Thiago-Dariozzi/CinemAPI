using Domain.Entities;

namespace Application.Interfaces;

public interface ITicketService
{
    Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct);
    Task<Ticket> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<Ticket> AddAsync(Ticket ticket, CancellationToken ct);
    Task UpdateAsync(Ticket ticket, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
