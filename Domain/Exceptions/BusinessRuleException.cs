namespace Domain.Exceptions;

/// <summary>
/// Una regla de negocio que no se cumple y que no es un conflicto con otro dato (ej. rol inexistente,
/// contraseña actual incorrecta al cambiarla). Se traduce a 400.
/// </summary>
public sealed class BusinessRuleException(string message) : DomainException(message);
