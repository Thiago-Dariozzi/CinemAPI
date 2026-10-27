namespace Application.DTOs.Purchases;

// Total = Subtotal + FeeAmount, guardados al comprar. CanCancel dice si el usuario de la request puede
// cancelarla ahora (el cliente, hasta 2 horas antes de la función; el staff, siempre).
public sealed record PurchaseResponse(
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
    bool CanCancel,
    IReadOnlyList<PurchaseTicketResponse> Tickets);
