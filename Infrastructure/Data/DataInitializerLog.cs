using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

// Mensajes de log del arranque generados en compilación (sin boxing ni armado del mensaje si el
// nivel está deshabilitado).
internal static partial class DataInitializerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Se hashearon {Count} contraseñas guardadas en texto plano.")]
    public static partial void LegacyPasswordsUpgraded(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No había ningún SuperAdmin activo: se creó o recuperó {Email} desde la configuración.")]
    public static partial void SuperAdminSeeded(this ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falta DemoUsers:Password en la configuración: no se crean los usuarios demo (ver README).")]
    public static partial void DemoPasswordMissing(this ILogger logger);
}
