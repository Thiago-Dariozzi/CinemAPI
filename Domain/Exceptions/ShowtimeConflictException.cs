using System;

namespace Domain.Exceptions;

/// <summary>
/// Dos funciones (Showtime) se solapan en la misma sala. El controller la traduce a 409
/// (Conflict), nunca a un 500.
/// </summary>
public class ShowtimeConflictException : Exception
{
    public ShowtimeConflictException(string message) : base(message)
    {
    }
}
