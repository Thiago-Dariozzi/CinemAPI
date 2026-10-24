using Application.DTOs.Tickets;

namespace Application.Interfaces;

public interface ITicketService
{
    Task<IReadOnlyList<TicketResponse>> GetAllAsync(CancellationToken ct);
    Task<TicketResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<TicketResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
