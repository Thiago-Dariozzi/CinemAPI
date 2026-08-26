using System;

namespace Domain.Exceptions;

/// <summary>
/// Se lanza cuando una función (Showtime) se solapa en horario con otra función activa
/// de la misma sala. Es una violación de una regla de negocio, no un error inesperado:
/// el controller la debe traducir a un 409 (Conflict), nunca a un 500.
/// </summary>
public class ShowtimeConflictException : Exception
{
    public ShowtimeConflictException(string message) : base(message)
    {
    }
}
