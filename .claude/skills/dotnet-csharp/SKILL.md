---
name: dotnet-csharp
description: Buenas prácticas de C# y ASP.NET Core (.NET 10) para CinemAPI. Usar al escribir o modificar cualquier archivo .cs — servicios, controllers, entidades, async, nullability, nombres, DI, configuración, logging, fechas.
---

# C# / .NET 10 — buenas prácticas

## Nombres y estilo

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases, records, métodos, propiedades, constantes | PascalCase | `ShowtimeService`, `CleaningBufferMinutes` |
| Interfaces | `I` + PascalCase | `IShowtimeService` |
| Campos privados | `_camelCase` | `_showtimeRepository` |
| Parámetros y variables | camelCase | `screenId` |
| Métodos asíncronos | sufijo `Async` (excepto acciones de controllers, ver abajo) | `GetByIdAsync` |
| Archivos | un tipo público por archivo, mismo nombre | `Showtime.cs` |

- Namespaces file-scoped (`namespace Domain.Entities;`).
- Sin `using System;` ni otros usings que ya cubre `ImplicitUsings`; sin usings sin uso.
- Clases `sealed` por defecto (servicios, controllers, DTOs) salvo que se hereden.
- Constructores primarios para inyección cuando el tipo solo guarda dependencias: `public sealed class GenreService(IGenreRepository genres) : IGenreService`. No reasignar parámetros capturados.
- Nombres corregidos si tienen errores de tipeo (ej. archivo `SeatCaregory.cs` → `SeatCategory.cs`).

## Async

```csharp
// ❌
public async Task<IEnumerable<Showtime>> GetAll() => await _repo.GetAll();
var movie = _movieRepo.GetById(id).Result;

// ✅
public async Task<IReadOnlyList<ShowtimeResponse>> GetAllAsync(CancellationToken ct)
{
    var showtimes = await _showtimeRepository.GetAllAsync(ct);
    return showtimes.Select(s => s.ToResponse()).ToList();
}
```

- Propagar `CancellationToken` desde el controller (ASP.NET lo inyecta como parámetro) hasta cada llamada a EF (`ToListAsync(ct)`, `SaveChangesAsync(ct)`).
- Nunca `.Result`, `.Wait()`, `async void` (salvo event handlers), ni `Task.Run` para "hacer async" algo síncrono.
- No hace falta `ConfigureAwait(false)` en ASP.NET Core.
- Un `await` por operación de I/O; no llamar a la base dentro de un `foreach` (N+1).

## Nullability

- `Nullable` está activado: respetar las advertencias, no silenciarlas.
- No usar `!` (null-forgiving) salvo con un comentario que explique por qué es seguro (ej. hoy `created!.Id` en `UserController` es innecesario si `Add` no devuelve nullable).
- Métodos que pueden no encontrar algo: `Task<T?>` en repositorio; en servicio, convertir a `NotFoundException`.
- `string.Empty` para defaults de strings requeridos; `string?` solo si de verdad es opcional (ej. `Synopsis`).

## Colecciones y LINQ

- Devolver `IReadOnlyList<T>` (materializado) en vez de `IEnumerable<T>` diferido desde servicios y repositorios.
- No enumerar dos veces el mismo `IEnumerable` (ej. `.Count()` y después recorrerlo).
- `Any()` en vez de `Count() > 0`.

## Fechas y horas

- No usar `DateTime.Now`/`DateTime.UtcNow` en lógica de negocio: inyectar `TimeProvider` (`builder.Services.AddSingleton(TimeProvider.System)`) y usar `timeProvider.GetUtcNow()`. Permite testear.
- Rango de un día: `start = date.Date; end = start.AddDays(1); where x >= start && x < end`.
- Formatos de fecha para mensajes al usuario con cultura explícita (`CultureInfo.GetCultureInfo("es-AR")`) si se formatea en el backend.

## Excepciones

- Excepciones de dominio para reglas de negocio, heredando de una base `DomainException`; una por concepto (`ShowtimeConflictException`, `GenreConflictException`, `NotFoundException`).
- No capturar `Exception` genérica para "seguir de largo". No usar excepciones para control de flujo normal.
- No tirar `ShowtimeConflictException` cuando el problema es otro (ej. "película no encontrada" es `NotFoundException`, no conflicto).
- Mensajes en español, claros para el usuario final, sin datos sensibles.

## Inyección de dependencias

- Servicios y repositorios `Scoped` (igual que el `DbContext`). `TimeProvider` Singleton.
- Nunca inyectar un servicio `Scoped` dentro de un `Singleton` (dependencia cautiva).
- Registrar por interfaz: `services.AddScoped<IShowtimeService, ShowtimeService>()`.

## Configuración

- Cadena de conexión y orígenes CORS en `appsettings.json`/`appsettings.Development.json` (sección `Cors:AllowedOrigins`), leídos con `builder.Configuration`.
- Para settings con varios valores usar el Options pattern (`services.AddOptions<CorsSettings>().BindConfiguration("Cors").ValidateOnStart()`).
- Secretos (contraseñas de DB reales, claves) con `dotnet user-secrets` o variables de entorno, nunca en el repo.
- El seeder solo corre en `Development`: `if (app.Environment.IsDevelopment()) { ... }`.

## Logging

- Inyectar `ILogger<T>`; plantillas estructuradas, no interpolación: `logger.LogWarning("Showtime {ShowtimeId} overlaps", id)`.
- Loguear errores inesperados una sola vez (en el `GlobalExceptionHandler`), no en cada capa.
- Nunca loguear contraseñas ni datos personales completos.

## Controllers (ASP.NET Core)

- `[ApiController]` + rutas en minúscula consistentes; parámetros de ruta tipados (`{id:guid}`).
- Acciones **sin** sufijo `Async` (`GetById`, `Create`): ASP.NET Core recorta "Async" del nombre de la acción y `CreatedAtAction(nameof(GetByIdAsync), ...)` deja de encontrar la ruta. El `.editorconfig` exime a `WebApi/Controllers` de esa regla.
- Parámetros de query obligatorios: si `date` es requerido, validarlo (`[FromQuery, BindRequired] DateOnly date` o devolver 400), no dejar que llegue `default(DateTime)`.
- Devolver `ActionResult<T>` con `CreatedAtAction` en POST, `NoContent` en PUT/DELETE, `Ok` en GET.

## Seguridad básica (dentro del alcance de la materia)

- Nunca devolver `Password` en una respuesta (ya existe `UserResponseDto`: usarlo siempre).
- Nunca armar SQL concatenando strings; EF ya parametriza.
- Contraseñas en texto plano: deuda conocida; no implementar auth sin pedido explícito, pero no empeorarlo (no loguearlas, no exponerlas, no aceptar `Password` en el PUT si no corresponde).
