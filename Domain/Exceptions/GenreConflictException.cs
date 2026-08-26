using System;

namespace Domain.Exceptions;

/// <summary>
/// Se lanza cuando una operación sobre Genre viola una regla de negocio: un nombre
/// equivalente ya existe (duplicado por may/min o tildes), o se intenta borrar un género
/// que todavía tiene películas asignadas. El controller la debe traducir a un 409
/// (Conflict), nunca a un 500.
/// </summary>
public class GenreConflictException : Exception
{
    public GenreConflictException(string message) : base(message)
    {
    }
}
