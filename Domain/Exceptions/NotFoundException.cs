namespace Domain.Exceptions;

/// <summary>
/// El recurso pedido no existe o está dado de baja. Se traduce a 404.
/// </summary>
public sealed class NotFoundException(string entityName, object key)
    : DomainException($"No se encontró {entityName} con identificador '{key}'.");
