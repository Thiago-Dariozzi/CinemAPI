using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TicketRepository : BaseRepository<Ticket>, ITicketRepository
{
    public TicketRepository(CinemAPIContext context) : base(context)
    {
    }
    public new async Task<IEnumerable<Ticket>> GetAll()
    {
        return await _context.Tickets
        .Where(t => t.IsActive)
        .ToListAsync();
    }

    public new async Task<Ticket?> GetById(Guid id)
    {
        return await _context.Tickets
        .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
    }

    public new async Task<Ticket> Add(Ticket ticket)
    {
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public new async Task Update(Ticket ticket)
    {
        // Ver comentario equivalente en BaseRepository.Update: evita el conflicto de
        // tracking con la instancia que dejó trackeada el GetById del controller.
        _context.ChangeTracker.Clear();
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync();
    }

    public new async Task Delete(Guid id)
    {
        var ticket = await GetById(id);
        if (ticket != null)
        {
            ticket.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Ticket>> GetByUserId(Guid userId)
    {
        return await _context.Tickets
        .Where(t => t.UserId == userId && t.IsActive)
        .ToListAsync();
    }

}