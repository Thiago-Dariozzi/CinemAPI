namespace Domain.Models;

// Una compra con lo que hace falta para mostrarla (película, sala, horario, cliente y butacas) en una
// sola consulta. Incluye datos de películas, salas y usuarios dados de baja: la compra existió igual.
public sealed record PurchaseDetails(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    Guid ShowtimeId,
    DateTime StartTime,
    Guid MovieId,
    string MovieTitle,
    Guid ScreenId,
    string ScreenName,
    DateTime CreatedAt,
    decimal Subtotal,
    decimal FeeAmount,
    decimal Total,
    string Status,
    DateTime? CancelledAt,
    IReadOnlyList<PurchasedSeat> Seats);

public sealed record PurchasedSeat(Guid TicketId, int Row, int Number, decimal Price);
