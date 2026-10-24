using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class TicketService(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
{
    public async Task<IEnumerable<Ticket>> GetAll()
    {
        return await ticketRepository.GetAll();
    }

    public async Task<Ticket?> GetById(Guid id)
    {
        return await ticketRepository.GetById(id);
    }

    public async Task<Ticket> Add(Ticket ticket)
    {
        ticket.Id = Guid.NewGuid();
        ticket.IsActive = true;
        ticketRepository.Add(ticket);
        await unitOfWork.SaveChangesAsync();
        return ticket;
    }

    public async Task Update(Ticket ticket)
    {
        var existing = await ticketRepository.GetById(ticket.Id);
        if (existing == null)
        {
            return;
        }

        existing.MovieId = ticket.MovieId;
        existing.ScreenId = ticket.ScreenId;
        existing.UserId = ticket.UserId;
        existing.BuyDate = ticket.BuyDate;
        existing.FinalPrice = ticket.FinalPrice;
        existing.IsActive = ticket.IsActive;
        await unitOfWork.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var ticket = await ticketRepository.GetById(id);
        if (ticket != null)
        {
            ticket.IsActive = false;
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Ticket>> GetByUserId(Guid userId)
    {
        return await ticketRepository.GetByUserId(userId);
    }
}
