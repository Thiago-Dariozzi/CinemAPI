namespace Domain.Exceptions;

/// <summary>
/// La base rechazó un guardado por un índice único. Pasa cuando dos pedidos simultáneos superan
/// la validación del servicio; cada servicio puede atraparla y responder su propio mensaje.
/// Si llega al handler global, se traduce a 409 (Conflict), nunca a un 500.
/// </summary>
public sealed class UniqueConstraintConflictException(Exception innerException)
    : ConflictException("Ya existe un registro con esos datos.", innerException);
