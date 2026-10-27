namespace Domain.Models;

// Una butaca de una sala: fila (1 = "A") y número dentro de la fila.
public sealed record Seat(int Row, int Number);
