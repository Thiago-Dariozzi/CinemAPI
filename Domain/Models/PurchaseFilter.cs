namespace Domain.Models;

// Filtros del listado de ventas. Las fechas son de la compra, en [From, To).
public sealed record PurchaseFilter(Guid? MovieId, Guid? ShowtimeId, DateTime? From, DateTime? To, string? Status);
