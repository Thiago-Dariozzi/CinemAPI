namespace Domain.Exceptions;

/// <summary>
/// El usuario autenticado no puede hacer esta operación sobre este recurso (ej. un Admin que
/// intenta crear otro Admin, o un cliente que pide datos de otro). Se traduce a 403.
/// </summary>
public sealed class ForbiddenException(string message) : DomainException(message);
