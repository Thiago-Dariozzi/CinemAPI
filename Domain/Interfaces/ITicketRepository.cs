using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces;

// "Vendida" = entrada activa y no cancelada.
public interface ITicketRepository : IBaseRepository<Ticket>
{
    Task<IReadOnlyList<Seat>> GetTakenSeatsAsync(Guid showtimeId, CancellationToken ct);

    Task<bool> HasSoldTicketsAsync(Guid showtimeId, CancellationToken ct);

    // ¿Hay entradas vendidas para funciones de la sala que empiezan después de 'after' en butacas fuera
    // de Rows × SeatsPerRow? (no se puede achicar la sala).
    Task<bool> HasSoldSeatsOutsideAsync(Guid screenId, int rows, int seatsPerRow, DateTime after, CancellationToken ct);
}
