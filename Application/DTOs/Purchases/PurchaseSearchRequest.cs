namespace Application.DTOs.Purchases;

// Filtros del listado de ventas (query string). From y To son días de compra, ambos incluidos.
// Status: Confirmed o Cancelled.
public sealed record PurchaseSearchRequest(Guid? MovieId, Guid? ShowtimeId, DateOnly? From, DateOnly? To, string? Status);
