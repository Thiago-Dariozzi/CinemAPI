namespace Domain.Models;

// Lo necesario para calcular solapamientos sin pedir la película de cada función (N+1).
// DurationMinutes y MovieTitle son null si la película ya no está activa.
public sealed record ShowtimeOccupancy(Guid ShowtimeId, DateTime StartTime, int? DurationMinutes, string? MovieTitle);
