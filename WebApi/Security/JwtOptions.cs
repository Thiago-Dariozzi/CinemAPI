using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Security;

// Sección "Jwt" de la configuración. Key va en user-secrets en desarrollo, nunca en appsettings.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 8;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}
