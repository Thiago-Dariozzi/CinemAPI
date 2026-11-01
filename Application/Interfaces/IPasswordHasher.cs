namespace Application.Interfaces;

// Application no conoce ASP.NET Identity: la implementación (PasswordHasher<User>) vive en Infrastructure.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}
