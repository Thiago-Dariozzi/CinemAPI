using Domain.Entities;

namespace Application.Interfaces;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

// La implementación (JWT) vive en WebApi, que es quien conoce el esquema de autenticación.
public interface ITokenGenerator
{
    AccessToken Create(User user);
}
