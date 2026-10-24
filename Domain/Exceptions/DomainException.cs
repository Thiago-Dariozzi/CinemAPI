namespace Domain.Exceptions;

/// <summary>
/// Base de las violaciones de reglas de negocio. El GlobalExceptionHandler de WebApi traduce
/// cada tipo a su código HTTP; el mensaje está pensado para mostrarse al usuario.
/// </summary>
public abstract class DomainException(string message) : Exception(message);
