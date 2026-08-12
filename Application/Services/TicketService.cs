using System;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class TicketService
{
    private readonly ITicketRepository _repo;

    public TicketService(ITicketRepository ticketRepository)
    {
        _repo = ticketRepository;
    }
    public async Task<IEnumerable<Ticket>> GetAll()
    {
        return await _repo.GetAll();
    }
    public async Task<Ticket?> GetById(Guid id)
    {
        return await _repo.GetById(id);
    }
    public async Task<Ticket> Add(Ticket ticket)
    {
        ticket.Id = Guid.NewGuid();
        ticket.IsActive = true;
        return await _repo.Add(ticket);
    }
    public async Task Update(Ticket ticket)
    {
        await _repo.Update(ticket);
    }
    public async Task Delete(Guid id)
    {
        var ticket = await _repo.GetById(id);
        if (ticket != null)
        {
            ticket.IsActive = false;
            await _repo.Update(ticket);
        }
    }
    public async Task<IEnumerable<Ticket>> GetByUserId(Guid userId)
    {
        return await _repo.GetByUserId(userId);
    }
}