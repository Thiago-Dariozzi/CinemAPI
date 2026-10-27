namespace Domain.Models;

// Butacas de una función: capacidad de la sala (filas × butacas por fila) y entradas vendidas.
public sealed record ShowtimeSeatsStat(
    Guid ShowtimeId,
    DateTime StartTime,
    Guid MovieId,
    string MovieTitle,
    Guid ScreenId,
    string ScreenName,
    int Capacity,
    int SoldSeats);
