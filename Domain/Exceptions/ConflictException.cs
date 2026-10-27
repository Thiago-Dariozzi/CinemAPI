namespace Domain.Exceptions;

/// <summary>
/// Base de los conflictos con el estado actual de los datos (duplicados, solapamientos, butacas
/// ocupadas, una compra ya cancelada). El GlobalExceptionHandler la traduce a 409 (Conflict).
/// </summary>
public abstract class ConflictException(string message, Exception? innerException = null) : DomainException(message, innerException);
