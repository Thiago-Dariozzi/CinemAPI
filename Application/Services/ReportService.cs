using Application.DTOs.Reports;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// Reportes del staff. Las sumas y los conteos los hace la base (IReportRepository); acá solo se arman los
// totales con las filas ya agrupadas (una por película o por función) y se calculan los porcentajes.
public sealed class ReportService(IReportRepository reportRepository) : IReportService
{
    // Un rango más largo sigue funcionando, pero deja de ser un reporte útil y pesa en la base.
    public const int MaxSalesRangeDays = 366;

    public async Task<SalesReportResponse> GetSalesAsync(SalesReportRequest request, CancellationToken ct)
    {
        var from = request.From.GetValueOrDefault();
        var to = DateRules.EnsureHasNextDay(request.To.GetValueOrDefault());
        if (from > to)
        {
            throw new BusinessRuleException("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");
        }

        if (to.DayNumber - from.DayNumber >= MaxSalesRangeDays)
        {
            throw new BusinessRuleException($"El rango del reporte no puede superar los {MaxSalesRangeDays} días.");
        }

        // Días completos: [from 00:00, to + 1 día 00:00).
        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var byMovie = (await reportRepository.GetSalesByMovieAsync(start, end, ct))
            .OrderByDescending(m => m.Total)
            .ThenBy(m => m.MovieTitle)
            .Select(m => new MovieSalesResponse(m.MovieId, m.MovieTitle, m.Purchases, m.TicketsSold, m.Subtotal, m.FeeAmount, m.Total))
            .ToList();
        var cancelled = await reportRepository.CountCancelledTicketsAsync(start, end, ct);

        return new SalesReportResponse(
            from,
            to,
            byMovie.Sum(m => m.Purchases),
            byMovie.Sum(m => m.TicketsSold),
            cancelled,
            byMovie.Sum(m => m.TicketRevenue),
            byMovie.Sum(m => m.FeesCollected),
            byMovie.Sum(m => m.TotalRevenue),
            byMovie);
    }

    // La capacidad es la de la sala hoy: si se achicó después de una función pasada, su porcentaje puede
    // pasar el 100 % (se informa tal cual, no se recorta).
    public async Task<OccupancyReportResponse> GetOccupancyAsync(OccupancyReportRequest request, CancellationToken ct)
    {
        var date = DateRules.EnsureHasNextDay(request.Date.GetValueOrDefault());
        var start = date.ToDateTime(TimeOnly.MinValue);
        var showtimes = (await reportRepository.GetShowtimeSeatsAsync(start, start.AddDays(1), ct))
            .Select(s => new ShowtimeOccupancyResponse(
                s.ShowtimeId, s.StartTime, s.MovieId, s.MovieTitle, s.ScreenId, s.ScreenName,
                s.Capacity, s.SoldSeats, Percentage(s.SoldSeats, s.Capacity)))
            .ToList();

        var totalSeats = showtimes.Sum(s => s.TotalSeats);
        var soldSeats = showtimes.Sum(s => s.SoldSeats);
        return new OccupancyReportResponse(date, totalSeats, soldSeats, Percentage(soldSeats, totalSeats), showtimes);
    }

    private static decimal Percentage(int part, int whole) =>
        whole == 0 ? 0m : Math.Round(part * 100m / whole, 1, MidpointRounding.AwayFromZero);
}
