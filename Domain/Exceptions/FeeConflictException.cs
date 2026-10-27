namespace Domain.Exceptions;

/// <summary>
/// Otro cargo quedó vigente al mismo tiempo (dos pedidos simultáneos marcaron uno distinto). 409 (Conflict).
/// </summary>
public sealed class FeeConflictException(string message) : ConflictException(message);
