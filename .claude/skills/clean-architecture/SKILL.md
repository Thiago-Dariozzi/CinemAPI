---
name: clean-architecture
description: Reglas de Clean Architecture de CinemAPI (Domain, Application/Services, Infrastructure/Repositories, WebApi/Controllers). Usar al crear o mover clases, entidades, servicios, repositorios, DTOs, controllers, registrar dependencias o decidir en qué capa va algo.
---

# Clean Architecture en CinemAPI

## Qué va en cada capa

| Capa | Contiene | NO contiene |
|---|---|---|
| **Domain** | Entidades (`Movie`, `Showtime`, ...), interfaces de repositorio (`IShowtimeRepository`), excepciones de dominio, reglas e invariantes de la entidad | EF Core, atributos `[Table]`/`[JsonProperty]`, DTOs, servicios, logging |
| **Application** | Servicios (`ShowtimeService`), interfaces de servicio (`IShowtimeService`), DTOs de request/response, mapeos entidad↔DTO, `DependencyInjection.cs` con `AddApplication()` | `CinemAPIContext`, `HttpContext`, `IActionResult`, SQL |
| **Infrastructure** | `CinemAPIContext`, configuraciones `IEntityTypeConfiguration<T>`, repositorios, migraciones, `DbSeeder`, `DependencyInjection.cs` con `AddInfrastructure(IConfiguration)` | Reglas de negocio (validar solapamientos, duplicados, etc.) |
| **WebApi** | Controllers, `GlobalExceptionHandler`, configuración de CORS/OpenAPI, `Program.cs` | Lógica de negocio, acceso directo a repositorios o al contexto |

## Estructura objetivo

```
Domain/
  Entities/            Movie.cs, Showtime.cs, ...
  Interfaces/          IBaseRepository.cs, IShowtimeRepository.cs, ...
  Exceptions/          DomainException.cs (base), ShowtimeConflictException.cs, NotFoundException.cs, ...
Application/
  DTOs/
    Showtimes/         CreateShowtimeRequest.cs, UpdateShowtimeRequest.cs, ShowtimeResponse.cs
  Interfaces/          IShowtimeService.cs, ...
  Mappings/            ShowtimeMappings.cs   (métodos de extensión ToResponse / ToEntity)
  Services/            ShowtimeService.cs, ...
  DependencyInjection.cs
Infrastructure/
  Configurations/      ShowtimeConfiguration.cs, ...
  Repositories/
  Migrations/
  Data/DbSeeder.cs
  CinemAPIContext.cs
  DependencyInjection.cs
WebApi/
  Controllers/
  ErrorHandling/GlobalExceptionHandler.cs
  Program.cs
```

## DTOs y mapeo (sin AutoMapper)

```csharp
// Application/DTOs/Showtimes/CreateShowtimeRequest.cs
public sealed record CreateShowtimeRequest(
    [property: Required] Guid? MovieId,        // Guid? + [Required]: con Guid a secas, [Required] no detecta el valor vacío
    [property: Required] Guid? ScreenId,
    [property: Required] DateTime? StartTime,
    [property: Range(0, 1_000_000)] decimal Price);

// Application/DTOs/Showtimes/ShowtimeResponse.cs
public sealed record ShowtimeResponse(Guid Id, Guid MovieId, Guid ScreenId, DateTime StartTime, decimal Price);

// Application/Mappings/ShowtimeMappings.cs
public static class ShowtimeMappings
{
    public static ShowtimeResponse ToResponse(this Showtime s) =>
        new(s.Id, s.MovieId, s.ScreenId, s.StartTime, s.Price);
}
```

- Request DTO = lo que el cliente puede mandar (sin `Id`, sin `IsActive`). Response DTO = lo que se expone (nunca `Password`).
- Mantener los mismos nombres de campo que hoy consume el front para no romper el contrato (JSON en camelCase por defecto).
- Validaciones de formato con DataAnnotations en el Request DTO: `[ApiController]` devuelve 400 automáticamente.

## Servicios (casos de uso)

```csharp
public interface IShowtimeService
{
    Task<IReadOnlyList<ShowtimeResponse>> GetAllAsync(CancellationToken ct);
    Task<ShowtimeResponse> GetByIdAsync(Guid id, CancellationToken ct);       // tira NotFoundException
    Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateShowtimeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
```

- El servicio recibe DTOs y devuelve DTOs; las entidades no salen de Application.
- Reglas de negocio (solapamiento de funciones, género duplicado, género con películas) viven en el servicio o en la entidad, nunca en el controller ni en el repositorio.
- Update: traer la entidad (trackeada), aplicar cambios, guardar. Nada de reemplazar la entidad entera con lo que manda el cliente.
- "No existe" → `NotFoundException`; regla de negocio violada → excepción de dominio específica (heredan de `DomainException`).

## Controllers delgados

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class ShowtimeController(IShowtimeService showtimeService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ShowtimeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShowtimeResponse>> Create(CreateShowtimeRequest request, CancellationToken ct)
    {
        var created = await showtimeService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
```

- Sin `try/catch`, sin `if` de negocio, sin chequeo previo de existencia (lo hace el servicio).
- Inyectar la **interfaz** del servicio, no la clase concreta.
- Tipar el retorno (`ActionResult<T>`) y documentar códigos con `ProducesResponseType`.

## Manejo global de errores

```csharp
// WebApi/ErrorHandling/GlobalExceptionHandler.cs
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ShowtimeConflictException or GenreConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            DomainException => (StatusCodes.Status400BadRequest, "Regla de negocio no cumplida"),
            _ => (StatusCodes.Status500InternalServerError, "Error inesperado")
        };
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(ex, "Unhandled exception");
        }

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new()
        {
            HttpContext = http,
            ProblemDetails = { Status = status, Title = title, Detail = status == 500 ? null : ex.Message }
        });
    }
}
// Program.cs: builder.Services.AddProblemDetails(); builder.Services.AddExceptionHandler<GlobalExceptionHandler>(); app.UseExceptionHandler();
```

Ojo: el front hoy lee el error con `response.text()`. Pasar a ProblemDetails es un cambio de contrato → actualizar el front para leer `detail` del JSON.

## Composition root

```csharp
// Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Cada proyecto registra lo suyo en su `DependencyInjection.cs`. `Program.cs` no conoce repositorios concretos.

## Violaciones típicas en este repo y su corrección

| Violación | Señal | Corrección |
|---|---|---|
| Entidad expuesta en la API | `[FromBody] Showtime`, `return Ok(showtime)` | Request/Response DTOs + mapeo en Application |
| try/catch de negocio en controller | `catch (GenreConflictException ex) { return Conflict(...) }` | `GlobalExceptionHandler` + ProblemDetails |
| Controller valida existencia | `GetById` + `NotFound()` antes de `Update`/`Delete` | El servicio tira `NotFoundException` |
| Controller depende de clase concreta | `ShowtimeService _showtimeService` | `IShowtimeService` registrado en `AddApplication()` |
| DI y configuración en `Program.cs` | `AddScoped<IUserRepository, UserRepository>()` en Program | `AddInfrastructure(configuration)` |
| Configuración EF en el contexto | `OnModelCreating` gigante | `IEntityTypeConfiguration<T>` + `ApplyConfigurationsFromAssembly` |
| Modelo anémico con reglas dispersas | `showtime.IsActive = false` desde servicio y repo | Método de entidad (`Deactivate()`) y un único lugar para la regla — cambio gradual, consultar antes de cerrar setters |
| Valores mágicos | `CLEANING_BUFFER_MINUTES` en el servicio | Constante PascalCase en el dominio (`Showtime.CleaningBufferMinutes`) o en opciones de configuración |

## Blindaje

El proyecto `CinemAPI.Tests` debe tener tests de arquitectura que verifiquen la regla de dependencias (ver skill `testing-dotnet`).
