# Auditoría backend — CinemAPI

Rama `refactor/buenas-practicas` (base `main` @ `06a46f4`, kit en `5bb9e24`). Punto de partida: `docs/hallazgos-iniciales.md` (H1–H21); cada ítem se confirmó contra el código y se agregaron los nuevos (marcados **nuevo**).

## Línea de base

| Verificación | Resultado |
|---|---|
| `dotnet build CinemAPI.slnx` | OK, **98 warnings únicos**: IDE0005 ×43, IDE1006 ×39, CA1304 ×4, CA1311 ×4, IDE0011 ×3, CA1862 ×2, CA1849 ×1, CA1051 ×1, NU1903 ×1 |
| `dotnet test CinemAPI.slnx` | No hay proyecto de tests |
| `dotnet format --verify-no-changes` | Falla: 45 de espacios, 22 archivos sin salto de línea final, más los fixes de estilo de arriba |

## Hallazgos

Severidades: **Crítica** (seguridad, pérdida de datos, bug) · **Alta** (viola la arquitectura) · **Media** (mantenibilidad/rendimiento) · **Baja** (estilo).

| # | Sev. | Archivo:línea | Regla | Problema | Corrección propuesta | ¿Contrato/esquema? | Lote | Estado |
|---|---|---|---|---|---|---|---|---|
| 1 | Crítica | Todos los `Update*` de controllers (`[FromBody] Entidad`) | Request DTOs | Asignación masiva (H1 ampliado): un PUT puede cambiar `IsActive`, el `Role` de un usuario (pasarlo a `Admin`), su `Password`, o el `UserId`/`FinalPrice` de un ticket | Request DTOs sin `Id`/`IsActive`; el servicio trae la entidad y copia solo los campos permitidos | Sí, compatible (el JSON de más se ignora) | 6 | pendiente |
| 2 | Crítica | `UserController.cs:56` + `CinemAPIContext.cs:59` | Bug | **Nuevo.** El índice único de `Email` incluye usuarios dados de baja, pero el chequeo previo solo busca activos → alta con el email de un usuario inactivo = `DbUpdateException` → **500** | Chequear existencia ignorando el filtro de baja → 409 (sin tocar el esquema) | Sí: 500 → 409 | 8 | pendiente |
| 3 | Crítica | `MovieService.Add/Update`, `TicketService.Add/Update`, `ShowtimeService.ValidateNoOverlap` | Bug | **Nuevo.** No se validan las referencias: `GenreId`, `MovieId`, `ScreenId` o `UserId` inexistentes → violación de FK → **500**. Las referencias dadas de baja (género o sala inactivos) se aceptan. `Showtime` valida la película pero **no la sala** | Validar en el servicio → 400 con mensaje claro | Sí: 500 → 400 | 8 | pendiente |
| 4 | Crítica | `ShowtimeService.cs:91` | Bug | **Nuevo.** El solapamiento solo mira funciones del mismo día calendario: una función 23:00 de 120 min (termina 01:15) no choca con otra a las 00:30 del día siguiente. El comentario dice que no hay funciones que crucen la medianoche, pero nada lo impide | Buscar candidatas en la ventana `[inicio − duración máxima, fin)` (o día anterior + mismo día) | Sí: más 409 (regla más estricta) | 8 | pendiente |
| 5 | Crítica | `ShowtimeService.cs:102-106` | Bug | **Nuevo.** Si la película de una función existente fue dada de baja, `GetById` devuelve `null`, se usa una duración de 0 min y la función solo "ocupa" 15 min → se aceptan funciones superpuestas | Leer la duración ignorando el filtro de baja (la función sigue activa y ocupa la sala) | Sí: más 409 | 8 | pendiente |
| 6 | Crítica | `UserController.cs:71` + front `UserCard.jsx:13` | Bug | **Nuevo.** El front edita usuarios con `password: ""` → `[ApiController]` responde 400 (`Password` requerido): **editar un usuario desde el front no funciona**. Si se escribe una contraseña, la pisa | `UpdateUserRequest` sin `Password` (cambio de contraseña queda fuera, con la deuda de auth) | Sí, compatible | 6 | pendiente |
| 7 | Crítica | `WebApi.csproj:10` | Seguridad | **Nuevo.** NU1903: `Microsoft.OpenApi` 2.0.0 (transitivo de `Microsoft.AspNetCore.OpenApi` 10.0.4) tiene una vulnerabilidad alta conocida. Solo se expone en Development, pero además bloquea `TreatWarningsAsErrors` | Subir `Microsoft.AspNetCore.OpenApi` al último 10.0.x | No (versión de paquete: **requiere OK**) | 2 | pendiente |
| 8 | Alta | `GenreController.cs:41-94`, `ShowtimeController.cs:60-94` | Errores | H2: `try/catch` por endpoint para excepciones de dominio | `GlobalExceptionHandler` (`IExceptionHandler`). En el lote 5 se conserva el cuerpo actual de los errores (texto plano) | No (lote 5) | 5 | pendiente |
| 9 | Alta | Todos los controllers, `Program.cs:31-36` | Capas | H3: los controllers dependen de servicios concretos | `I<Nombre>Service` en `Application/Interfaces` + `AddApplication()` | No | 5 | pendiente |
| 10 | Alta | Todos los `Update*`/`Delete*` de controllers | Capas | H4: el controller hace `GetById` para validar existencia | El servicio tira `NotFoundException` → 404 en el handler | No | 5 | pendiente |
| 11 | Alta | `ShowtimeService.cs:84` | Errores | H5: película inexistente → `ShowtimeConflictException` (409) | Mismo tratamiento que el #3 (400) | Sí: 409 → 400 | 8 | pendiente |
| 12 | Alta | Todos los controllers | Contrato | H1: se devuelven entidades de dominio (solo `User` usa DTO) | Response DTOs + mapeos en `Application/Mappings`, con los mismos nombres de campo | No, si se mantienen los campos | 6 | pendiente |
| 13 | Alta | `Movie.IsActive` + front `movieFields.js:50`, `MovieCard.jsx:110` | Modelo | **Nuevo.** El front usa `isActive` como "En cartelera / Fuera de cartelera" (editable), pero en el backend `IsActive = false` es **baja lógica**: marcar una película "fuera de cartelera" la borra | Campo propio (ej. `IsShowing`) + migración, o quitar el toggle del front | Sí: esquema y contrato | — | **requiere decisión** |
| 14 | Media | `BaseRepository.cs:48` y 4 repos más | EF | H6: `ChangeTracker.Clear()` + `Update(entity)` | El servicio trae la entidad trackeada, la modifica y guarda | No | 3 | pendiente |
| 15 | Media | `Genre/Movie/Screen/Showtime/Ticket/UserRepository` (28 métodos) | EF | H7: métodos `new` que ocultan la base para filtrar `IsActive` | `HasQueryFilter(e => e.IsActive)` + `virtual`/`override` | No | 3 | pendiente |
| 16 | Media | Repositorios (15 llamadas) | EF | H8: cada `Add/Update/Delete` llama a `SaveChangesAsync` | `IUnitOfWork.SaveChangesAsync` una vez por caso de uso | No | 3 | pendiente |
| 17 | Media | `ShowtimeRepository.cs:70` | EF | H9: `s.StartTime.Date == date.Date` no usa índices | Rango `[día, día+1)` + índice `(ScreenId, StartTime)` | Esquema: índice (migración) | 3 | pendiente |
| 18 | Media | `ShowtimeService.cs:96-104` | EF | H10: N+1, busca la película de cada candidata en un loop | Una consulta con join que traiga la duración | No | 3 | pendiente |
| 19 | Media | `GenreService.cs:82` | EF | H11: trae todos los géneros (trackeados) para comparar en memoria | **Propuesta:** mantener la comparación en el servicio (proyectando solo `Id`/`Name` sin tracking). Pasarla a la base cambia la semántica: con `Modern_Spanish_CI_AI` "Año" ≠ "Ano", con `IgnoreNonSpace` son iguales | No | 3 | pendiente |
| 20 | Media | Toda la solución | Async | H12: sin `CancellationToken`, 39 métodos sin sufijo `Async`, retornos `IEnumerable` diferidos | Propagar `CancellationToken`, renombrar, `IReadOnlyList<T>` | No | 4 | pendiente |
| 21 | Media | `Program.cs:14-23` | Config | H13: CORS `http://localhost:5173` hardcodeado | `Cors:AllowedOrigins` en appsettings | No | 2 | pendiente |
| 22 | Media | `Program.cs:30-43` | Capas | H13: DI de repositorios y servicios en `Program.cs` | `AddApplication()` / `AddInfrastructure(configuration)` | No | 2 | pendiente |
| 23 | Media | `Program.cs:46-50` | Config | H14: `DbSeeder` corre en todos los ambientes | Solo en Development | No | 2 | pendiente |
| 24 | Media | `CinemAPIContext.cs:19-82` | EF | H15: toda la configuración en `OnModelCreating` | `IEntityTypeConfiguration<T>` + `ApplyConfigurationsFromAssembly` (migración vacía) | No | 3 | pendiente |
| 25 | Media | `ShowtimeController.cs:50` | Contrato | H16: `date` puede faltar y queda en `01/01/0001` | Obligatorio → 400 | Sí (validación nueva) | 6 | pendiente |
| 26 | Media | Solución | Tests | H17: no hay proyecto de tests | `CinemAPI.Tests` (caracterización + arquitectura) | No | 1 | pendiente |
| 27 | Media | `MovieRepository.cs:46`, `UserRepository.cs:29` | EF | **Nuevo.** `ToLower() == ToLower()` impide usar índices (y genera CA1304/CA1311/CA1862). La columna ya compara sin distinguir mayúsculas (collation CI por defecto) | `m.Title == title` dejando la comparación a la collation | No | 3 | pendiente |
| 28 | Media | `BaseRepository.cs:9,16-24` | EF | **Nuevo.** `FindAsync` ignora el filtro de baja si la entidad ya está trackeada; `GetAll` sin `AsNoTracking`; campo `protected` visible (CA1051) | `FirstOrDefaultAsync` + `AsNoTracking` + propiedad `Context` | No | 3 | pendiente |
| 29 | Media | Controllers / entidades | Validación | **Nuevo.** Sin validación de formato: un título de más de 150 caracteres (o un nombre de más de 50) hace que la base trunque → **500**; se aceptan capacidad, duración y precio negativos | DataAnnotations (`StringLength`, `Range`) en los Request DTOs → 400 | Sí (400 nuevos) | 6 | pendiente |
| 30 | Media | `GenreService.cs:55-57` | EF | **Nuevo.** Para saber si un género tiene películas trae la lista completa y la cuenta | `CountAsync` en el repositorio | No | 3 | pendiente |
| 31 | Media | `DbSeeder.cs:87-97` | Datos | **Nuevo.** El seeder genera funciones superpuestas en la misma sala (viola la regla de negocio) | Generar horarios sin solapamiento | No | 3 | pendiente |
| 32 | Media | `Infrastructure.csproj:5-7`, `WebApi.csproj:11-15` | Paquetes | **Nuevo.** Versiones de EF desalineadas: 10.0.9 en Infrastructure, 10.0.11 en Design/Tools | Alinear a una sola versión 10.0.x | No (**requiere OK**) | 2 | pendiente |
| 33 | Baja | `MovieController.cs:52` | Contrato | H18: `POST` devuelve `Movie?` con 200 | `CreatedAtAction` → 201 | Sí: 200 → 201 (el front solo mira `ok`) | 6 | pendiente |
| 34 | Baja | 43 usings en 33 archivos | Estilo | H19: `using System;` y otros sin uso (IDE0005) | Quitar | No | 2 | pendiente |
| 35 | Baja | `GenreService.cs:53,73`, `ShowtimeService.cs:98` | Estilo | **Nuevo.** `if` sin llaves (IDE0011) | Agregar llaves | No | 2 | pendiente |
| 36 | Baja | `ShowtimeService.cs:14` | Estilo | H19: constante `CLEANING_BUFFER_MINUTES` | `CleaningBufferMinutes` (en el lote 7 del kit, a `Showtime`) | No | 2 | pendiente |
| 37 | Baja | Todas las clases | Estilo | H19: servicios, controllers, repositorios y excepciones sin `sealed` | `sealed` salvo las que se heredan | No | 2 | pendiente |
| 38 | Baja | `Domain/Entities/SeatCaregory.cs` | Estilo | H20: typo en el nombre del archivo | Renombrar a `SeatCategory.cs` | No | 2 | pendiente |
| 39 | Baja | `UserController.cs:63` | Nullability | H21: `created!.Id` | Firma no nullable en el servicio | No | 5 | pendiente |
| 40 | Baja | 45 bloques / 22 archivos | Formato | **Nuevo.** Espacios y saltos de línea finales (`dotnet format`) | `dotnet format` | No | 2 | pendiente |
| 41 | Baja | `Program.cs:62` | Async | **Nuevo.** `app.Run()` bloquea (CA1849) | `await app.RunAsync()` | No | 2 | pendiente |
| 42 | Baja | `GenreConflictException.cs:5`, `ShowtimeConflictException.cs:5`, `ShowtimeService.cs:40`, `ShowtimeRepository.cs:8,47` | Comentarios | **Nuevo.** Comentarios que quedan falsos con el refactor ("el controller la traduce a 409", "reusado por ValidateNoOverlap", "re-implemento con new") | Actualizarlos en el lote que cambia ese código (no se borran en masa) | No | 2–5 | pendiente |
| 43 | Baja | Los 4 `.csproj` | Config | **Nuevo.** `Nullable`/`ImplicitUsings` duplicados con `Directory.Build.props` | Quitar de los `.csproj` | No | 2 | pendiente |
| 44 | Baja | `Screen.cs:10`, `Ticket.cs:13` | Modelo | **Nuevo.** `IsActive` sin `= true` (las demás entidades sí lo tienen) | `= true` (el servicio y el seeder ya lo setean: sin cambio observable) | No | 2 | pendiente |
| 45 | Baja | `GenreService.Add` / índice único | Errores | **Nuevo.** Dos altas simultáneas del mismo género pasan el chequeo y la segunda revienta el índice único → 500 | Traducir la violación de índice único a 409 en el handler | Sí: 500 → 409 | — | pendiente (bajo impacto) |
| 46 | Baja | Todos los controllers | Contrato | **Nuevo.** Rutas `{id}` sin `:guid`: un id mal formado da 400 en vez de 404 | `{id:guid}` | Sí: 400 → 404 en ids inválidos | 6 | pendiente |

**Totales: 7 Críticas · 6 Altas · 19 Medias · 14 Bajas (46).**

## Plan de lotes

Cada lote: build + test + format en verde, migración revisada si cambió el modelo, commit con Conventional Commits. Los tests del lote 1 tienen que pasar antes y después de cada lote (salvo los que el lote 8 cambia a propósito, avisando cuáles).

| Lote | Contenido | Ítems | ¿Contrato? | Front |
|---|---|---|---|---|
| 1 | `CinemAPI.Tests` (xUnit, fakes escritos a mano, sin NSubstitute) + caracterización de `ShowtimeService` y `GenreService` + tests de arquitectura | 26 | No | — |
| 2 | Estilo y configuración: `dotnet format`, usings, llaves, `sealed`, constante, typo, `.csproj`, CORS a appsettings, seeder solo en Development, `AddApplication`/`AddInfrastructure`, `RunAsync`, paquetes (si das el OK) | 7, 21–23, 32, 34–38, 40–44 | No | — |
| 3 | EF interno: configuraciones separadas (migración vacía), `HasQueryFilter`, sin `new` ni `Clear()`, `IUnitOfWork`, rango por día + índice (migración `AddShowtimeScreenStartTimeIndex`), join contra el N+1, `CountAsync`, `ToLower`, seeder sin solapamientos | 14–19, 24, 27, 28, 30, 31 | No (índice nuevo) | — |
| 4 | Async + `CancellationToken` de punta a punta, sufijo `Async`, `IReadOnlyList<T>` | 20 | No | — |
| 5 | Interfaces de servicio, `NotFoundException`, controllers delgados, `GlobalExceptionHandler` **conservando el cuerpo actual** de los errores (409 en texto plano, 404 como hoy) | 8–10, 39 | No | — |
| 6 | **Contrato A:** Request/Response DTOs con los mismos nombres de campo, DataAnnotations, `{id:guid}`, `date` obligatorio, `POST` de película → 201, `PUT` de usuario sin contraseña | 1, 6, 12, 25, 29, 33, 46 | Sí | `movieApi.js` / `screenApi.js` solo si hace falta; `userApi.js` deja de mandar `password` en el PUT |
| 7 | **Contrato B:** ProblemDetails para todos los errores | (8) | Sí | Todos los `src/api/*.js` leen `detail` del JSON |
| 8 | **Bugs de negocio** con un test que los reproduce: email de usuario inactivo (409), referencias inexistentes o inactivas (400), solapamiento que cruza la medianoche, película dada de baja, película inexistente en una función (409 → 400) | 2–5, 11 | Sí (status) | Mensajes nuevos |
| — | Cierre: `TreatWarningsAsErrors`, revisión de `revisor-backend` | — | — | — |

No se tocan (se reportan): #13, #45 y la deuda conocida de abajo. El "dominio más rico" (lote 7 de la skill) queda como deuda, porque la skill pide confirmación antes de cerrar los setters.

## Deuda conocida — requiere decisión (no se toca)

- **Contraseñas en texto plano** (`UserService.Add`).
- **`Ticket` referencia `MovieId` + `ScreenId` en vez de `ShowtimeId`.**
- **`Fee` y `SeatCategory`** usan `int Id`, no están en el contexto ni se usan.
- **Modelo anémico** (setters públicos).
- **Idioma de los comentarios.**
- **#13 `Movie.IsActive` usado como "en cartelera"** (nuevo).
- **Qué pasa con las funciones de una película dada de baja** (hoy siguen activas y se listan). El #5 solo arregla el solapamiento.

## Hallazgos fuera de alcance (front)

- `cine-frontend/src/api/screenApi.js:17` · `getActiveScreens` llama a `/api/screen/active`, que no existe en el backend (hoy da 400). No se usa en ningún componente: se puede borrar.
- `CinemAPIFront/CLAUDE.md` y `CinemAPIFront/.claude/` están fuera del repo git (`cine-frontend/`): no quedan versionados.
