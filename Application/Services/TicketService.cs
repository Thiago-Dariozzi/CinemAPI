using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class TicketService(ITicketRepository ticketRepository, IUnitOfWork unitOfWork) : ITicketService
{
    private const string EntityName = "el ticket";

    public Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct) => ticketRepository.GetAllAsync(ct);

    public async Task<Ticket> GetByIdAsync(Guid id, CancellationToken ct) =>
        await ticketRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    public Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        ticketRepository.GetByUserIdAsync(userId, ct);

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
        var existing = await GetByIdAsync(ticket.Id, ct);

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
        var ticket = await GetByIdAsync(id, ct);
        ticket.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
