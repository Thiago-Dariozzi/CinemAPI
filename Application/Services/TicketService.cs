using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class TicketService(ITicketRepository ticketRepository, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct)
    {
        return await ticketRepository.GetAllAsync(ct);
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await ticketRepository.GetByIdAsync(id, ct);
    }

    public async Task<Ticket> AddAsync(Ticket ticket, CancellationToken ct)
    {
        ticket.Id = Guid.NewGuid();
        ticket.IsActive = true;
        ticketRepository.Add(ticket);
        await unitOfWork.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct)
    {
        var existing = await ticketRepository.GetByIdAsync(ticket.Id, ct);
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
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var ticket = await ticketRepository.GetByIdAsync(id, ct);
        if (ticket != null)
        {
            ticket.IsActive = false;
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await ticketRepository.GetByUserIdAsync(userId, ct);
    }
}
