namespace Application.DTOs.Showtimes;

// Las funciones de una película en un día (hora local del cine), ordenadas por horario.
public sealed record ShowtimeDayResponse(DateOnly Date, IReadOnlyList<ShowtimeAvailabilityResponse> Showtimes);
