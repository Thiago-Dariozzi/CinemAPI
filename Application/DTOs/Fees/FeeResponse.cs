namespace Application.DTOs.Fees;

// Percentage: 10 = 10 % del subtotal. FixedAmount: monto fijo por compra.
public sealed record FeeResponse(Guid Id, string Name, decimal Percentage, decimal FixedAmount, bool IsCurrent, bool IsActive);
