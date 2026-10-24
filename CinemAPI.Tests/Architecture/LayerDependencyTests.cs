using Microsoft.EntityFrameworkCore;

namespace CinemAPI.Tests.Architecture;

// GetReferencedAssemblies solo lista lo que el código compilado usa de verdad, así que estos
// tests detectan una violación real y no una referencia olvidada en el .csproj.
public sealed class LayerDependencyTests
{
    private static List<string> ReferencesOf(Type typeInAssembly) =>
        typeInAssembly.Assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

    private static bool IsFramework(string name) =>
        name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal);

    [Fact]
    public void Domain_DoesNotDependOnOtherLayersOrFrameworks()
    {
        var references = ReferencesOf(typeof(global::Domain.Entities.Movie));

        Assert.DoesNotContain(references, n => n is "Application" or "Infrastructure" or "WebApi" || IsFramework(n));
    }

    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        var references = ReferencesOf(typeof(global::Application.Services.ShowtimeService));

        Assert.DoesNotContain(references, n => n is "Infrastructure" or "WebApi" || IsFramework(n));
    }

    [Fact]
    public void Controllers_DoNotDependOnRepositoriesOrDbContext()
    {
        var constructorParameters = typeof(global::WebApi.Controllers.MovieController).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .SelectMany(t => t.GetConstructors())
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.NotEmpty(constructorParameters);
        Assert.DoesNotContain(constructorParameters, p =>
            p.Name.EndsWith("Repository", StringComparison.Ordinal) || typeof(DbContext).IsAssignableFrom(p));
    }
}
