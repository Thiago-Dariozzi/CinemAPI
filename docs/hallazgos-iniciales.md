# Hallazgos iniciales — CinemAPI (backend)

Revisión hecha sobre `main` (commit `06a46f4`) al armar la configuración de Claude Code. Sirve de punto de partida para `/auditar-backend`, que debe confirmar cada ítem, buscar más y corregir por lotes.

| # | Sev. | Dónde | Problema | Corrección | ¿Contrato/esquema? |
|---|---|---|---|---|---|
| 1 | Alta | Todos los controllers (`[FromBody] Movie/Showtime/Screen/Ticket/Genre/User`) | Reciben y devuelven entidades de dominio; el cliente puede mandar `Id`, `IsActive`, `Password` | Request/Response DTOs por entidad + mapeo en Application (patrón ya iniciado con `UserResponseDto`) | Sí (mantener nombres de campos para minimizar impacto) |
| 2 | Alta | `ShowtimeController`, `GenreController` | `try/catch` por endpoint para excepciones de dominio | `GlobalExceptionHandler` (`IExceptionHandler`) + `AddProblemDetails()` | Sí: el cuerpo del error pasa a ProblemDetails JSON; el front lee `response.text()` |
| 3 | Alta | Controllers + `Program.cs` | Controllers dependen de servicios concretos (`ShowtimeService`) | `I<Nombre>Service` en Application, registro en `AddApplication()` | No |
| 4 | Alta | Controllers (PUT/DELETE) | El controller hace `GetById` para validar existencia antes de llamar al servicio | El servicio tira `NotFoundException` → 404 en el handler global | No |
| 5 | Alta | `ShowtimeService.ValidateNoOverlap` | Película inexistente se informa como `ShowtimeConflictException` (409) | `NotFoundException` / 400 según corresponda | Sí (status code) |
| 6 | Media | `BaseRepository` y todos los repositorios | `ChangeTracker.Clear()` + `Update(entity)` para esquivar el doble tracking | Servicio trae la entidad trackeada, la modifica y guarda | No |
| 7 | Media | `Screen/Genre/Ticket/Showtime/Movie/UserRepository` | Métodos `new` que ocultan los de la base para filtrar `IsActive` | `HasQueryFilter(e => e.IsActive)` + `virtual/override` | No |
| 8 | Media | Repositorios | Cada `Add/Update/Delete` llama a `SaveChangesAsync` | `IUnitOfWork.SaveChangesAsync` una vez por caso de uso | No |
| 9 | Media | `ShowtimeRepository.GetByScreenAndDate` | `s.StartTime.Date == date.Date` no usa índices | Filtro por rango `[día, día+1)` + índice `(ScreenId, StartTime)` | Esquema: índice nuevo (migración) |
| 10 | Media | `ShowtimeService.ValidateNoOverlap` | N+1: busca la película de cada candidata en un loop | Una consulta con join para traer duración | No |
| 11 | Media | `GenreService.EnsureNoDuplicate` | Trae todos los géneros para comparar en memoria | `AnyAsync` en la base (la columna ya es `Modern_Spanish_CI_AI`) | No |
| 12 | Media | Toda la solución | Sin `CancellationToken`; métodos sin sufijo `Async`; retornos `IEnumerable` diferidos | Propagar `CancellationToken`, renombrar, `IReadOnlyList<T>` | No |
| 13 | Media | `Program.cs` | CORS `http://localhost:5173` hardcodeado; DI de repositorios en Program | `Cors:AllowedOrigins` en appsettings; `AddInfrastructure(configuration)` | No |
| 14 | Media | `Program.cs` | `DbSeeder` corre en todos los ambientes | Solo en Development | No |
| 15 | Media | `CinemAPIContext.OnModelCreating` | Toda la configuración en un solo método | `IEntityTypeConfiguration<T>` + `ApplyConfigurationsFromAssembly` (migración debe salir vacía) | No |
| 16 | Media | `ShowtimeController.GetByScreen` | `date` puede llegar vacío (`01/01/0001`) sin error | `DateOnly` obligatorio o 400 | Sí (validación nueva) |
| 17 | Media | Solución | No hay proyecto de tests | `CinemAPI.Tests` con tests de reglas (solapamiento, géneros) y de arquitectura | No |
| 18 | Baja | `MovieController.Add` | Devuelve `Task<Movie?>` en vez de `ActionResult` con 201 | `ActionResult<MovieResponse>` + `CreatedAtAction` | Sí (status 200 → 201) |
| 19 | Baja | Varios | `using System;` redundante, constante `CLEANING_BUFFER_MINUTES` en mayúsculas, clases no `sealed` | Limpieza de estilo | No |
| 20 | Baja | `Domain/Entities/SeatCaregory.cs` | Typo en el nombre del archivo (la clase es `SeatCategory`) | Renombrar archivo | No |
| 21 | Baja | `UserController.CreateUser` | `created!.Id` con null-forgiving | Firma no nullable en el servicio | No |

## Deuda conocida — requiere decisión (no corregir sin pedido)

- **Contraseñas en texto plano** (`UserService.Add`): la materia todavía no vio autenticación. Opción cuando corresponda: `PasswordHasher<User>` de `Microsoft.Extensions.Identity.Core` + migrar datos existentes.
- **`Ticket` referencia `MovieId` + `ScreenId` en vez de `ShowtimeId`**: no permite saber a qué función corresponde la entrada. Es cambio de esquema y de contrato.
- **`Fee` y `SeatCategory` usan `int Id`** mientras el resto usa `Guid`, y no están en el contexto: definir si se usan.
- **Modelo anémico** (todas las propiedades con `public set`): mover reglas a métodos de entidad de forma gradual.
- **Idioma de los comentarios**: en el front se pidió quitar los comentarios en español; confirmar si aplica al backend.
