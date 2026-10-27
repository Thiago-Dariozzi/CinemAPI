namespace Domain.Exceptions;

/// <summary>
/// Login fallido o contraseña actual incorrecta. Se traduce a 401. El mensaje no dice si falló
/// el email o la contraseña.
/// </summary>
public sealed class InvalidCredentialsException(string message = "Email o contraseña incorrectos.") : DomainException(message);
