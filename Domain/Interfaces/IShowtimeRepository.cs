using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces;

public interface IShowtimeRepository : IBaseRepository<Showtime>
{
    Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId);

    // Funciones que empiezan ese día calendario; la hora que venga en date se ignora.
    Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date);

    Task<IEnumerable<ShowtimeOccupancy>> GetOccupancyByScreenAndDate(Guid screenId, DateTime date);
}
