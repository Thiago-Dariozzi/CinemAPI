using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces;

// "Vendida" = entrada activa y no cancelada.
public interface ITicketRepository : IBaseRepository<Ticket>
{
    Task<IReadOnlyList<Seat>> GetTakenSeatsAsync(Guid showtimeId, CancellationToken ct);

    Task<bool> HasSoldTicketsAsync(Guid showtimeId, CancellationToken ct);

    // Entradas vendidas por función, de las funciones de la película que empiezan después de 'after', en una
    // sola consulta. Las funciones sin ventas no aparecen.
    Task<IReadOnlyDictionary<Guid, int>> CountSoldByShowtimeAsync(Guid movieId, DateTime after, CancellationToken ct);

    // ¿Hay entradas vendidas para funciones de la sala que empiezan después de 'after' en butacas fuera
    // de Rows × SeatsPerRow? (no se puede achicar la sala).
    Task<bool> HasSoldSeatsOutsideAsync(Guid screenId, int rows, int seatsPerRow, DateTime after, CancellationToken ct);
}
