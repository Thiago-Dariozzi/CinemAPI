using Application.DTOs.Tickets;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class TicketService(ITicketRepository ticketRepository, IUnitOfWork unitOfWork) : ITicketService
{
    private const string EntityName = "el ticket";

    public async Task<IReadOnlyList<TicketResponse>> GetAllAsync(CancellationToken ct) =>
        ToResponses(await ticketRepository.GetAllAsync(ct));

    public async Task<TicketResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<IReadOnlyList<TicketResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        ToResponses(await ticketRepository.GetByUserIdAsync(userId, ct));

    public async Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct)
    {
        var ticket = request.ToEntity();
        ticket.Id = Guid.NewGuid();
        ticket.IsActive = true;
        ticketRepository.Add(ticket);
        await unitOfWork.SaveChangesAsync(ct);
        return ticket.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct)
    {
        var ticket = await GetEntityAsync(id, ct);
        request.ApplyTo(ticket);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var ticket = await GetEntityAsync(id, ct);
        ticket.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Ticket> GetEntityAsync(Guid id, CancellationToken ct) =>
        await ticketRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    private static List<TicketResponse> ToResponses(IEnumerable<Ticket> tickets) =>
        tickets.Select(t => t.ToResponse()).ToList();
}
