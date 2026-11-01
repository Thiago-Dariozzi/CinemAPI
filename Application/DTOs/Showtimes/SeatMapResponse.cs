namespace Application.DTOs.Showtimes;

// El mapa de una función: la sala es Rows × SeatsPerRow y TakenSeats son las vendidas. IsOnSale dice
// si hoy se pueden comprar entradas (la función no empezó y la película está en cartelera).
public sealed record SeatMapResponse(
    Guid ShowtimeId,
    Guid MovieId,
    string MovieTitle,
    Guid ScreenId,
    string ScreenName,
    DateTime StartTime,
    decimal Price,
    int Rows,
    int SeatsPerRow,
    bool IsOnSale,
    IReadOnlyList<SeatResponse> TakenSeats);
