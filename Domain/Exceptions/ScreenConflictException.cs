namespace Domain.Exceptions;

/// <summary>
/// La sala no se puede achicar porque hay entradas vendidas para funciones futuras en butacas que
/// dejarían de existir. 409 (Conflict).
/// </summary>
public sealed class ScreenConflictException(string message) : ConflictException(message);
