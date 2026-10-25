using Application;
using Application.Interfaces;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CinemAPI.Tests.Application;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersEveryServiceInterface()
    {
        var registered = new ServiceCollection().AddApplication().Select(d => d.ServiceType).ToList();

        var serviceInterfaces = typeof(IMovieService).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Service", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(serviceInterfaces);
        Assert.All(serviceInterfaces, i => Assert.Contains(i, registered));
    }

    // ValidateOnBuild arma cada servicio sin conectarse a la base: detecta un repositorio sin registrar.
    [Fact]
    public void ApplicationAndInfrastructure_ResolveTheWholeGraph()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CinemaConnection"] = "Server=localhost;Database=NoSeUsa",
            })
            .Build();
        var services = new ServiceCollection().AddApplication().AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IShowtimeService>());
    }
}
