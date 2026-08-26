using System;
using Domain.Entities;
using Domain.Interfaces;

namespace Domain.Interfaces;

public interface IShowtimeRepository : IBaseRepository<Showtime>
{
    Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId);

    // Funciones activas de una sala en una fecha puntual (solo se compara la parte de
    // fecha de StartTime). La usa tanto el endpoint de "horarios ocupados" como la
    // validación de superposición en ShowtimeService.
    Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date);
}
