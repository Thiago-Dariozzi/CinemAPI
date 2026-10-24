using Domain.Entities;

namespace Domain.Interfaces;

public interface ITicketRepository : IBaseRepository<Ticket>
{
    Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
