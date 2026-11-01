namespace Domain.Exceptions;

/// <summary>
/// Violación de una regla de negocio de Genre (nombre duplicado, género con películas).
/// Se traduce a 409 (Conflict), nunca a un 500.
/// </summary>
public sealed class GenreConflictException(string message) : ConflictException(message);
