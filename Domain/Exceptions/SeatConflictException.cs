namespace Domain.Exceptions;

/// <summary>
/// Alguna de las butacas pedidas ya está vendida en esa función (o se vendió mientras se confirmaba
/// la compra). Se traduce a 409 (Conflict).
/// </summary>
public sealed class SeatConflictException(string message, Exception? innerException = null) : ConflictException(message, innerException);
