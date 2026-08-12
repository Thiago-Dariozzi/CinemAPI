using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TicketRepository : BaseRepository<Ticket>, ITicketRepository
{
    private readonly CinemAPIContext _context;
    public TicketRepository(CinemAPIContext context) : base(context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Ticket>> GetAll()
    {
        return await _context.Tickets
        .Where(t => t.IsActive)
        .ToListAsync();
    }

    public async Task<Ticket?> GetById(Guid id)
    {
        return await _context.Tickets
        .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
    }

    public async Task<Ticket> Add(Ticket ticket)
    {
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task Update(Ticket ticket)
    {
        _context.Tickets.Update(ticket);
        await _context.SaveChangesAsync();
    }
    
    public async Task Delete(Guid id)
    {
        var Ticket = await GetById(id);
        if (Ticket != null)
        {
            Ticket.IsActive = false;
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