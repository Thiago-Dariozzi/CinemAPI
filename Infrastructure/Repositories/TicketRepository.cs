using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TicketRepository(CinemAPIContext context) : BaseRepository<Ticket>(context), ITicketRepository
{
    public async Task<IEnumerable<Ticket>> GetByUserId(Guid userId) =>
        await Context.Tickets.AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync();
}
