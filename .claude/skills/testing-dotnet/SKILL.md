---
name: testing-dotnet
description: Cómo escribir y organizar tests en CinemAPI (xUnit, servicios con fakes, controllers, tests de arquitectura). Usar al crear o modificar tests, al corregir un bug, o cuando un cambio de comportamiento necesita cobertura.
---

# Tests del backend

## Proyecto

- Un proyecto `CinemAPI.Tests` (xUnit) agregado a `CinemAPI.slnx`, referenciando `Domain`, `Application`, `Infrastructure` y `WebApi`.
- Carpetas espejo: `CinemAPI.Tests/Application/Services/ShowtimeServiceTests.cs`, `CinemAPI.Tests/Architecture/LayerDependencyTests.cs`.
- El `.editorconfig` tiene una sección `[CinemAPI.Tests/**.cs]` que permite los guiones bajos del nombre (`CA1707`) y no exige sufijo `Async` en los métodos de test.
- Paquetes mínimos: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`. Para dobles: `NSubstitute` (preguntar antes de agregarlo; alternativa sin paquetes: fakes escritos a mano). Aserciones con `Assert` de xUnit (sin librerías de aserciones con licencia comercial).

## Qué testear primero (prioridad)

1. **Reglas de negocio de los servicios:** solapamiento de funciones (`ShowtimeService`), normalización y duplicados de géneros, no borrar género con películas, soft delete.
2. **Bugs corregidos:** cada bug arreglado lleva un test que fallaba antes.
3. **Tests de arquitectura:** la regla de dependencias entre capas.
4. Mapeos entidad ↔ DTO (que nunca salga `Password`).

## Convenciones

- Nombre: `Metodo_Escenario_ResultadoEsperado` → `CreateAsync_WhenShowtimeOverlapsExisting_ThrowsShowtimeConflictException`.
- Estructura Arrange / Act / Assert, un comportamiento por test.
- `[Theory]` + `[InlineData]` para casos de borde (tocarse justo en el borde NO es solapamiento).
- Fechas fijas con `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) o `DateTime` literales; nunca `DateTime.Now`.

```csharp
public sealed class ShowtimeServiceTests
{
    private readonly IShowtimeRepository _showtimes = Substitute.For<IShowtimeRepository>();
    private readonly IMovieRepository _movies = Substitute.For<IMovieRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ShowtimeService _sut;

    public ShowtimeServiceTests() => _sut = new ShowtimeService(_showtimes, _movies, _unitOfWork);

    [Theory]
    [InlineData("2026-11-01T18:00", true)]   // termina 20:15 (120 + 15 min) → pisa la de las 20:00
    [InlineData("2026-11-01T22:15", false)]  // empieza justo cuando termina la otra → no choca
    public async Task CreateAsync_DetectsOverlapCorrectly(string start, bool shouldConflict)
    {
        // Arrange: función existente 20:00; ambas películas de 120 min
        ...
        // Act
        var act = () => _sut.CreateAsync(request, CancellationToken.None);
        // Assert
        if (shouldConflict)
        {
            await Assert.ThrowsAsync<ShowtimeConflictException>(act);
        }
        else
        {
            await act();
        }
    }
}
```

## Tests de arquitectura (sin paquetes extra)

```csharp
public sealed class LayerDependencyTests
{
    private static IEnumerable<string?> ReferencesOf(Type typeInAssembly) =>
        typeInAssembly.Assembly.GetReferencedAssemblies().Select(a => a.Name);

    [Fact]
    public void Domain_DoesNotDependOnOtherLayersOrFrameworks()
    {
        var refs = ReferencesOf(typeof(Domain.Entities.Movie));
        Assert.DoesNotContain(refs, n => n is "Application" or "Infrastructure" or "WebApi"
            || n!.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Microsoft.AspNetCore"));
    }

    [Fact]
    public void Application_DependsOnlyOnDomain()
    {
        var refs = ReferencesOf(typeof(Application.Services.ShowtimeService));
        Assert.DoesNotContain(refs, n => n is "Infrastructure" or "WebApi"
            || n!.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Microsoft.AspNetCore"));
    }

    [Fact]
    public void Controllers_DoNotDependOnRepositoriesOrDbContext()
    {
        var controllerTypes = typeof(WebApi.Controllers.MovieController).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller"));
        foreach (var type in controllerTypes)
        {
            var ctorParams = type.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType);
            Assert.DoesNotContain(ctorParams, p => p.Name.EndsWith("Repository") || typeof(Microsoft.EntityFrameworkCore.DbContext).IsAssignableFrom(p));
        }
    }
}
```

`GetReferencedAssemblies` solo lista los ensamblados realmente usados en el código compilado, así que estos tests detectan una violación real, no una referencia olvidada en el `.csproj`.

## Integración (opcional)

- Para endpoints completos: `WebApplicationFactory<Program>` (agregar `public partial class Program;` al final de `Program.cs`).
- Base de datos real para integración: Testcontainers con SQL Server si hay Docker disponible. No usar el proveedor InMemory de EF para validar consultas (no respeta collation, índices filtrados ni SQL real).

## Regla

`dotnet test CinemAPI.slnx` en verde es parte de la Definition of Done. Nunca borrar ni marcar `Skip` un test que falla para "pasar": arreglar el código o avisar.
