namespace Domain.Exceptions;

/// <summary>
/// Violación de una regla de negocio de User (email ya registrado). Se traduce a 409 (Conflict).
/// </summary>
public sealed class UserConflictException(string message) : ConflictException(message);
