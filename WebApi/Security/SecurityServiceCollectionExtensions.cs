using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Security;

public static class SecurityServiceCollectionExtensions
{
    private const int MinimumKeyBytes = 32;

    // JWT + políticas por rol + usuario actual. La configuración inválida corta el arranque con un
    // mensaje que dice qué falta, en lugar de fallar en el primer login.
    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Faltan Jwt:Issuer y Jwt:Audience en la configuración.")
            .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= MinimumKeyBytes,
                $"Jwt:Key debe tener al menos {MinimumKeyBytes} bytes. En desarrollo: " +
                "dotnet user-secrets set \"Jwt:Key\" \"...\" --project WebApi (ver README).")
            .Validate(o => o.ExpirationHours > 0, "Jwt:ExpirationHours debe ser mayor a 0.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.CreateSigningKey(),
                    NameClaimType = ClaimNames.Name,
                    RoleClaimType = ClaimNames.Role,
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = RejectOutdatedTokenAsync };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Staff, policy => policy.RequireRole(Roles.Admin, Roles.SuperAdmin))
            .AddPolicy(Policies.SuperAdminOnly, policy => policy.RequireRole(Roles.SuperAdmin))
            .AddPolicy(Policies.ClientOnly, policy => policy.RequireRole(Roles.Client));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    // Un usuario dado de baja, o al que le cambiaron el rol, deja de poder usar ese token en el momento
    // (401: el front cierra la sesión y vuelve a pedir login, que emite un token con el rol nuevo).
    private static async Task RejectOutdatedTokenAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirstValue(ClaimNames.Subject);
        var tokenRole = context.Principal?.FindFirstValue(ClaimNames.Role);
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        var currentRole = Guid.TryParse(subject, out var userId)
            ? await authService.GetActiveRoleAsync(userId, context.HttpContext.RequestAborted)
            : null;
        if (currentRole is null || currentRole != tokenRole)
        {
            context.Fail("El usuario fue dado de baja o cambió de rol.");
        }
    }
}
