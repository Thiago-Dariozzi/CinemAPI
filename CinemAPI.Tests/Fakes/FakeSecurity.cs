using Application.Interfaces;
using Domain.Entities;

namespace CinemAPI.Tests.Fakes;

// El usuario "logueado" en un test de servicio. Se cambia el rol para probar cada permiso.
public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = Roles.Client;

    public FakeCurrentUser As(User user)
    {
        Id = user.Id;
        Role = user.Role;
        return this;
    }
}

// Hash reversible y predecible: alcanza para probar que el servicio hashea y verifica (el hash real
// se prueba aparte en PasswordHasherAdapterTests).
public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string passwordHash, string password) => passwordHash == Hash(password);
}

public sealed class FakeTokenGenerator : ITokenGenerator
{
    public static readonly DateTime ExpiresAt = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    public AccessToken Create(User user) => new($"token-de-{user.Id}", ExpiresAt);
}
