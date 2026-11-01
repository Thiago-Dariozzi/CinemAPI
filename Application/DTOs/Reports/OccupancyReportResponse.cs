namespace Application.DTOs.Reports;

// La ocupación del día es la de todas las butacas del día juntas (no el promedio de los porcentajes).
public sealed record OccupancyReportResponse(
    DateOnly Date,
    int TotalSeats,
    int SoldSeats,
    decimal OccupancyPercentage,
    IReadOnlyList<ShowtimeOccupancyResponse> Showtimes);
