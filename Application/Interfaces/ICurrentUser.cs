namespace Application.Interfaces;

/// <summary>
/// El usuario del token de la request actual. Los servicios deciden con estos datos qué puede ver o
/// modificar: nunca con un id que mande el cliente en el body o en la ruta.
/// </summary>
public interface ICurrentUser
{
    Guid Id { get; }
    string Role { get; }
}
