using System;
using Domain.Entities;
using Domain.Interfaces;

namespace Domain.Interfaces;

public interface IShowtimeRepository : IBaseRepository<Showtime>
{
    Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId);

    // Solo compara la parte de fecha de StartTime, no la hora.
    Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date);
}
