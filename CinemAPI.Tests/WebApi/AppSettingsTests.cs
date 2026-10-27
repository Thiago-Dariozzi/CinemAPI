using System.Text.Json;

namespace CinemAPI.Tests.WebApi;

// Un appsettings.json inválido no rompe el build ni los tests unitarios, pero la API no arranca.
public sealed class AppSettingsTests
{
    [Fact]
    public void AppSettings_IsValidJsonWithConnectionStringAndCorsOrigins()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "WebApi", "appsettings.json")));
        var root = document.RootElement;

        var connectionString = root.GetProperty("ConnectionStrings").GetProperty("CinemaConnection").GetString();
        Assert.Contains(@"(localdb)\MSSQLLocalDB", connectionString, StringComparison.Ordinal);
        Assert.NotEqual(0, root.GetProperty("Cors").GetProperty("AllowedOrigins").GetArrayLength());
    }

    // Los secretos van en user-secrets (desarrollo) o variables de entorno, nunca en el repo.
    [Fact]
    public void AppSettings_DoesNotContainSecrets()
    {
        var root = FindRepositoryRoot();
        foreach (var file in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            var content = File.ReadAllText(Path.Combine(root, "WebApi", file));

            Assert.DoesNotContain("\"Key\"", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Password", content, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CinemAPI.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("No se encontró CinemAPI.slnx.");
    }
}
