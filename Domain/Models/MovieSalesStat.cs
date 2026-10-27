namespace Domain.Models;

// Ventas confirmadas de una película en un período, ya sumadas en la base.
public sealed record MovieSalesStat(
    Guid MovieId,
    string MovieTitle,
    int Purchases,
    int TicketsSold,
    decimal Subtotal,
    decimal FeeAmount,
    decimal Total);
