namespace Application.DTOs.Purchases;

// Una entrada de la compra. Row 1 = fila "A".
public sealed record PurchaseTicketResponse(Guid Id, int Row, int Number, decimal Price);
