namespace Domain.Exceptions;

/// <summary>
/// El request referencia una entidad que no existe o está dada de baja (ej. una función con una
/// película borrada). Es un error del pedido, no del recurso de la URL: se traduce a 400.
/// </summary>
public sealed class InvalidReferenceException(string entityName, Guid id)
    : DomainException($"No se encontró {entityName} con Id {id} (no existe o se dio de baja).");
