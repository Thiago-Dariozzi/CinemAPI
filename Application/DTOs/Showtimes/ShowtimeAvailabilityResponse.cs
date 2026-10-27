namespace Application.DTOs.Showtimes;

// Una función de la cartelera: horario, sala, precio y cuántas butacas quedan libres.
public sealed record ShowtimeAvailabilityResponse(
    Guid Id,
    DateTime StartTime,
    Guid ScreenId,
    string ScreenName,
    decimal Price,
    int TotalSeats,
    int AvailableSeats);
