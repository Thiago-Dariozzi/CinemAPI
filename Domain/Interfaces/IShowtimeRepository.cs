using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces;

public interface IShowtimeRepository : IBaseRepository<Showtime>
{
    Task<IReadOnlyList<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct);

    // Funciones que empiezan después de 'after', de una película o de todas, ordenadas por horario.
    Task<IReadOnlyList<Showtime>> GetUpcomingAsync(Guid? movieId, DateTime after, CancellationToken ct);

    // Funciones que empiezan ese día calendario; la hora que venga en date se ignora.
    Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct);

    // Funciones activas de la sala que empiezan en [from, to), con la duración de su película.
    Task<IReadOnlyList<ShowtimeOccupancy>> GetOccupancyByScreenAsync(
        Guid screenId, DateTime from, DateTime to, CancellationToken ct);
}
