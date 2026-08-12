using System;
using Domain.Entities;
using Domain.Interfaces;

namespace Domain.Interfaces;

public interface ITicketRepository : IBaseRepository<Ticket>
{
    Task<IEnumerable<Ticket>> GetByUserId(Guid userId);
}