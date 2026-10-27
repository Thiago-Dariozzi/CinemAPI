using Domain.Interfaces;

namespace Domain.Entities;

public class User : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // Hash de PasswordHasher (PBKDF2 con salt). Nunca sale de la API.
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Client;
    public bool IsActive { get; set; } = true;
}
