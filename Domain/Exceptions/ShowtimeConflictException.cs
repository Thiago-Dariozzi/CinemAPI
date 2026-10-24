namespace Domain.Exceptions;

/// <summary>
/// Dos funciones (Showtime) se solapan en la misma sala. El controller la traduce a 409
/// (Conflict), nunca a un 500.
/// </summary>
public sealed class ShowtimeConflictException : Exception
{
    public ShowtimeConflictException(string message) : base(message)
    {
    }
}
