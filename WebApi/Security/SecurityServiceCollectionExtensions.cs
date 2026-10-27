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
                bearer.Events = new JwtBearerEvents { OnTokenValidated = RejectInactiveUserAsync };
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

    // Un usuario dado de baja deja de poder usar la API en el momento, aunque su token no haya vencido.
    private static async Task RejectInactiveUserAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirstValue(ClaimNames.Subject);
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        if (!Guid.TryParse(subject, out var userId) || !await authService.IsActiveUserAsync(userId, context.HttpContext.RequestAborted))
        {
            context.Fail("El usuario no existe o fue dado de baja.");
        }
    }
}
