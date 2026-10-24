using Domain.Entities;

namespace Domain.Interfaces;

public interface ITicketRepository : IBaseRepository<Ticket>
{
    Task<IEnumerable<Ticket>> GetByUserId(Guid userId);
}
