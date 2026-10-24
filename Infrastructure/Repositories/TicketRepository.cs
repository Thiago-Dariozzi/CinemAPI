using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TicketRepository(CinemAPIContext context) : BaseRepository<Ticket>(context), ITicketRepository
{
    public async Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        await Context.Tickets.AsNoTracking()
            .Where(t => t.UserId == userId)
            .ToListAsync(ct);
}
