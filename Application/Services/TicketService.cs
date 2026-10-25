using Application.DTOs.Tickets;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class TicketService(
    ITicketRepository ticketRepository,
    IMovieRepository movieRepository,
    IScreenRepository screenRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : ITicketService
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
        await EnsureReferencesExistAsync(ticket, previous: null, ct);

        ticket.Id = Guid.NewGuid();
        ticket.IsActive = true;
        ticketRepository.Add(ticket);
        await unitOfWork.SaveChangesAsync(ct);
        return ticket.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct)
    {
        var ticket = await GetEntityAsync(id, ct);
        var previous = (ticket.MovieId, ticket.ScreenId, ticket.UserId);

        request.ApplyTo(ticket);
        await EnsureReferencesExistAsync(ticket, previous, ct);
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

    // En una edición solo se validan las referencias que cambiaron: un ticket viejo de una
    // película dada de baja se puede seguir editando.
    private async Task EnsureReferencesExistAsync(
        Ticket ticket, (Guid MovieId, Guid ScreenId, Guid UserId)? previous, CancellationToken ct)
    {
        if (ticket.MovieId != previous?.MovieId && !await movieRepository.ExistsAsync(ticket.MovieId, ct))
        {
            throw new InvalidReferenceException("la película", ticket.MovieId);
        }

        if (ticket.ScreenId != previous?.ScreenId && !await screenRepository.ExistsAsync(ticket.ScreenId, ct))
        {
            throw new InvalidReferenceException("la sala", ticket.ScreenId);
        }

        if (ticket.UserId != previous?.UserId && !await userRepository.ExistsAsync(ticket.UserId, ct))
        {
            throw new InvalidReferenceException("el usuario", ticket.UserId);
        }
    }

    private static List<TicketResponse> ToResponses(IEnumerable<Ticket> tickets) =>
        tickets.Select(t => t.ToResponse()).ToList();
}
