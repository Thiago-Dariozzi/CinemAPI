using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

// Se ejecuta al arrancar la API, después de aplicar las migraciones. Los dos primeros pasos corren en
// todos los ambientes y son idempotentes; los datos de prueba, solo en Development.
public static class DataInitializer
{
    private const int MinimumPasswordLength = 8;

    public static async Task InitializeAsync(IServiceProvider services, bool seedDemoData, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var context = provider.GetRequiredService<CinemAPIContext>();
        var hasher = provider.GetRequiredService<IPasswordHasher>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataInitializer).FullName!);

        await UpgradeLegacyPasswordsAsync(context, hasher, logger, ct);
        await EnsureSuperAdminAsync(context, hasher, configuration, logger, ct);

        if (seedDemoData)
        {
            await DbSeeder.SeedAsync(context, hasher, configuration, logger, ct);
        }
    }

    // Las contraseñas de antes de la autenticación se guardaban en texto plano. El hash se calcula en
    // C# (PBKDF2), así que no se puede hacer en la migración SQL: se reemplazan acá, una sola vez.
    // Incluye usuarios dados de baja para que no quede ninguna contraseña legible en la base.
    private static async Task UpgradeLegacyPasswordsAsync(CinemAPIContext context, IPasswordHasher hasher, ILogger logger, CancellationToken ct)
    {
        var candidates = await context.Users.IgnoreQueryFilters()
            .Where(u => !u.PasswordHash.StartsWith(PasswordHasherAdapter.HashPrefix))
            .ToListAsync(ct);
        var legacy = candidates.Where(u => !PasswordHasherAdapter.IsHash(u.PasswordHash)).ToList();
        if (legacy.Count == 0)
        {
            return;
        }

        foreach (var user in legacy)
        {
            user.PasswordHash = hasher.Hash(user.PasswordHash);
        }

        await context.SaveChangesAsync(ct);
        logger.LegacyPasswordsUpgraded(legacy.Count);
    }

    // Sin un SuperAdmin activo nadie podría crear Admins ni cargos: se crea (o se recupera) con las
    // credenciales de configuración. En desarrollo van en user-secrets (ver README).
    private static async Task EnsureSuperAdminAsync(
        CinemAPIContext context, IPasswordHasher hasher, IConfiguration configuration, ILogger logger, CancellationToken ct)
    {
        if (await context.Users.AnyAsync(u => u.Role == Roles.SuperAdmin, ct))
        {
            return;
        }

        var section = configuration.GetSection("SeedSuperAdmin");
        var email = section["Email"]?.Trim();
        var password = section["Password"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || password.Length < MinimumPasswordLength)
        {
            throw new InvalidOperationException(
                "No hay ningún SuperAdmin activo y faltan SeedSuperAdmin:Email y SeedSuperAdmin:Password " +
                $"(mínimo {MinimumPasswordLength} caracteres) en la configuración. En desarrollo: " +
                "dotnet user-secrets set \"SeedSuperAdmin:Email\" \"...\" --project WebApi (ver README).");
        }

        // Si el email ya existe (por ejemplo, el SuperAdmin fue dado de baja) se recupera esa cuenta
        // en lugar de chocar contra el índice único.
        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), Email = email };
            context.Users.Add(user);
        }

        user.Name = section["Name"] ?? "Super Admin";
        user.Role = Roles.SuperAdmin;
        user.IsActive = true;
        user.PasswordHash = hasher.Hash(password);
        await context.SaveChangesAsync(ct);
        logger.SuperAdminSeeded(email);
    }
}
