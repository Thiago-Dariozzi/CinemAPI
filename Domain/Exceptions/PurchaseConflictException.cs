namespace Domain.Exceptions;

/// <summary>
/// La compra no admite la operación por su estado (por ejemplo, cancelarla dos veces). 409 (Conflict).
/// </summary>
public sealed class PurchaseConflictException(string message) : ConflictException(message);
