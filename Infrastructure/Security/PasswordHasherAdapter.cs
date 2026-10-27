using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security;

// PasswordHasher<User> de Microsoft.Extensions.Identity.Core: PBKDF2 con salt aleatorio y el formato
// versionado de ASP.NET Identity, sin traer el resto de Identity.
public sealed class PasswordHasherAdapter : global::Application.Interfaces.IPasswordHasher
{
    // Todo hash v3 empieza con el byte 0x01 seguido de la PRF en big endian: en base64, "AQAAAA".
    internal const string HashPrefix = "AQAAAA";

    // El usuario no participa del cálculo en la implementación por defecto.
    private static readonly User NoUser = new();

    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(NoUser, password);

    public bool Verify(string passwordHash, string password) =>
        IsHash(passwordHash) && _hasher.VerifyHashedPassword(NoUser, passwordHash, password) != PasswordVerificationResult.Failed;

    // Distingue un hash de una contraseña vieja guardada en texto plano: base64 válido con la marca
    // de versión 3 y el largo mínimo del formato (marca + PRF + iteraciones + largo de salt).
    internal static bool IsHash(string value)
    {
        if (!value.StartsWith(HashPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out var written) && written > 13 && buffer[0] == 0x01;
    }
}
