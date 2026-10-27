using Domain.Models;

namespace Domain.Interfaces;

// Consultas de solo lectura para los reportes del staff. Todo se agrega en la base: nunca se traen las
// compras o las entradas a memoria para sumarlas. Los períodos son [start, end) en hora local del cine.
public interface IReportRepository
{
    // Compras confirmadas hechas en el período, agrupadas por película.
    Task<IReadOnlyList<MovieSalesStat>> GetSalesByMovieAsync(DateTime start, DateTime end, CancellationToken ct);

    // Entradas de compras hechas en el período que después se cancelaron.
    Task<int> CountCancelledTicketsAsync(DateTime start, DateTime end, CancellationToken ct);

    // Funciones activas que empiezan en el período, con su capacidad y sus entradas vendidas.
    Task<IReadOnlyList<ShowtimeSeatsStat>> GetShowtimeSeatsAsync(DateTime start, DateTime end, CancellationToken ct);
}
