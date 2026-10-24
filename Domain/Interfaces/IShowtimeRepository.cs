using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces;

public interface IShowtimeRepository : IBaseRepository<Showtime>
{
    Task<IReadOnlyList<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct);

    // Funciones que empiezan ese día calendario; la hora que venga en date se ignora.
    Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct);

    Task<IReadOnlyList<ShowtimeOccupancy>> GetOccupancyByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct);
}
