namespace Domain.Exceptions;

/// <summary>
/// Dos funciones (Showtime) se solapan en la misma sala. Se traduce a 409 (Conflict),
/// nunca a un 500.
/// </summary>
public sealed class ShowtimeConflictException(string message) : ConflictException(message);
