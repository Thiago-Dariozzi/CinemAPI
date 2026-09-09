using System;

namespace Domain.Exceptions;

/// <summary>
/// Violación de una regla de negocio de Genre. El controller la traduce a 409 (Conflict),
/// nunca a un 500.
/// </summary>
public class GenreConflictException : Exception
{
    public GenreConflictException(string message) : base(message)
    {
    }
}
