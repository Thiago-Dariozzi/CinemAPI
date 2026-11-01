namespace Domain.Models;

// Lo necesario para calcular solapamientos sin pedir la película de cada función (N+1).
// Incluye la duración aunque la película esté dada de baja: la función sigue ocupando la sala.
public sealed record ShowtimeOccupancy(Guid ShowtimeId, DateTime StartTime, int DurationMinutes, string MovieTitle);
