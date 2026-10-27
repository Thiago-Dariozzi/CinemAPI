# CLAUDE.md — CinemAPI (backend)

Sistema de gestión para un complejo de cines. Proyecto académico: el alcance lo marca la materia.
Este archivo se carga en cada sesión. Las reglas detalladas viven en `.claude/skills/` y se cargan solas cuando el tema lo requiere.

## Stack real del repo

| Tema | Valor |
|---|---|
| Runtime | .NET 10 (`net10.0`), C# con `Nullable` e `ImplicitUsings` activados |
| Solución | `CinemAPI.slnx` |
| Proyectos | `Domain`, `Application`, `Infrastructure`, `WebApi`, `CinemAPI.Tests` |
| Base de datos | SQL Server LocalDB vía EF Core 10 (`CinemAPIContext`), migraciones en `Infrastructure/Migrations` |
| Datos de prueba | `Infrastructure/Data/DbSeeder.cs` con Bogus |
| API | Controllers (`[ApiController]`, `api/[controller]`), OpenAPI nativo (`AddOpenApi`) |
| Casos de uso | Clases de servicio en `Application/Services` que implementan `I<Nombre>Service` (`Application/Interfaces`). NO usamos MediatR/CQRS |
| Persistencia | Repositorios: interfaces en `Domain/Interfaces` (`IBaseRepository<T>` con `GetAllAsync`/`GetByIdAsync`/`ExistsAsync`/`Add`), implementación en `Infrastructure/Repositories`. Guardar: `IUnitOfWork.SaveChangesAsync` (lo implementa `CinemAPIContext`), una vez por caso de uso |
| Configuración EF | `Infrastructure/Configurations/<Entidad>Configuration.cs` (`IEntityTypeConfiguration<T>`), aplicadas con `ApplyConfigurationsFromAssembly` |
| Errores de negocio | Excepciones en `Domain/Exceptions` que heredan de `DomainException`. `WebApi/ErrorHandling/GlobalExceptionHandler` las traduce a ProblemDetails: `NotFoundException` → 404, `ConflictException` y sus derivadas (`*ConflictException`) → 409, `InvalidReferenceException` y otras → 400. El mensaje para el usuario va en `detail` |
| DTOs | `Application/DTOs/<Plural>/`: `Create<X>Request`, `Update<X>Request`, `<X>Response` (records, uno por archivo). Mapeo manual en `Application/Mappings/<X>Mappings.cs` (`ToResponse`, `ToEntity`, `ApplyTo`) |
| Soft delete | Todas las entidades implementan `IEntity` y usan `IsActive`; borrar = `IsActive = false`, y un `HasQueryFilter(e => e.IsActive)` lo oculta de todas las lecturas. `Movie.IsShowing` ("en cartelera") es otra cosa |
| Frontend que consume la API | repo `CinemAPIFront` (React + Vite), CORS para `http://localhost:5173` |

## Decisiones tomadas (no cambiar sin pedido explícito)

- Se mantiene el estilo **Services + Repositories + Controllers**. No introducir MediatR, CQRS, AutoMapper, FluentValidation ni otras librerías "de arquitectura": se usan las herramientas que trae .NET (DataAnnotations, `IExceptionHandler`, `ProblemDetails`, mapeo manual).
- Autenticación: **JWT propio** (`Microsoft.AspNetCore.Authentication.JwtBearer`) + `PasswordHasher<User>` (`Microsoft.Extensions.Identity.Core`). Nada de ASP.NET Identity completo. Roles `SuperAdmin`, `Admin`, `Client` (`Domain/Entities/Roles.cs`).
- Los Ids son `Guid` generados en el servicio, nunca confiados del cliente.
- Soft delete con `IsActive` en todas las entidades.
- Identificadores (clases, métodos, variables, rutas) en **inglés**. Mensajes al usuario en **español**.
- Comentarios: solo para explicar el **porqué** (no el qué). <!-- PENDIENTE confirmar con el profe: idioma de los comentarios (en el front se pidió quitar los comentarios en español). Hasta confirmarlo: no borrar comentarios existentes en masa. -->

## Convenciones decididas en la auditoría (`docs/auditoria-backend.md`)

- **Request DTOs:** las validaciones van en los **parámetros** del record (`[Required] string Name`), nunca con `[property: ...]`: MVC responde 500. Para Guid/fecha obligatorios usar `Guid?`/`DateTime?` + `[Required]`. Mensajes en español. Los requests nunca tienen `Id` ni `IsActive`; el de usuario en el PUT no tiene `Password`. Lo verifica `ContractTests`.
- **Controllers:** `[Route("api/[controller]")]`, rutas con `{id:guid}`, `ActionResult<T>` + `ProducesResponseType`, `CreatedAtAction` en POST (201), `NoContent` en PUT/DELETE. Las acciones **no** llevan sufijo `Async` (ASP.NET lo recorta y rompe `CreatedAtAction`); el resto de los métodos async sí.
- **Servicios:** reciben y devuelven DTOs; "no existe" → `NotFoundException`; una referencia del body que no existe o está dada de baja → `InvalidReferenceException` (400). En la edición solo se validan las referencias que cambian.
- **Consultas:** lecturas con `AsNoTracking`, `GetByIdAsync` trackeado para modificar; `IgnoreQueryFilters()` solo a propósito y comentado (ej. el email único cuenta usuarios dados de baja; la ocupación de una sala incluye películas dadas de baja).
- **Tests:** `CinemAPI.Tests/` en carpetas espejo; fakes en memoria en `CinemAPI.Tests/Fakes` (sin librerías de mocking); integración contra **LocalDB** con una base temporal que aplica las migraciones (`SqlServerDatabaseFixture`). Nunca el proveedor InMemory.
- **Verificación de cambios en la API:** además de los tests, levantar la API contra una base LocalDB aparte y probar los endpoints tocados con `Origin: http://localhost:5173` (estado, content-type, CORS, cuerpo). Los tests unitarios no detectan configuración inválida, consultas no traducibles ni errores de model binding.
- **Build:** `TreatWarningsAsErrors` está activo, sin excepciones (incluye los avisos de paquetes vulnerables NU190x). `Infrastructure` referencia directamente `System.Security.Cryptography.Pkcs` para forzar la versión parcheada (#47).
- **Autorización:** políticas `Staff` (Admin + SuperAdmin), `SuperAdminOnly` y `ClientOnly` (`WebApi/Security/Policies.cs`). Cada controller lleva `[Authorize(Policy = ...)]` a nivel de clase y `[AllowAnonymous]` solo en las lecturas públicas del catálogo. Si el controller tiene endpoints que comparten clientes y staff (`PurchaseController`, `FeeController`), la clase lleva `[Authorize]` y **cada endpoint exclusivo lleva su política** (ej. `GET /api/purchase` → `Staff`, `POST /api/purchase/mine/{id}/cancel` → `ClientOnly`); cuando la misma acción cambia según el rol, se separa en dos rutas con su política en vez de decidirlo con un `if` del rol. Las reglas que dependen del dato (un cliente solo ve lo suyo, un Admin no administra Admins) van en el **servicio**, con `ICurrentUser` (el id y el rol salen del token, nunca del body ni de la ruta) y `ForbiddenException` → 403. `InvalidCredentialsException` → 401, `BusinessRuleException` → 400.
- **Secretos:** `Jwt:Key`, `SeedSuperAdmin:*` y `DemoUsers:Password` van en `dotnet user-secrets` (ver `README.md`); `AppSettingsTests` falla si aparecen en `appsettings*.json`. La API valida `Jwt` al arrancar.
- **Arranque:** `DataInitializer` hashea contraseñas viejas en texto plano y crea el SuperAdmin inicial (todos los ambientes); `DbSeeder` (Bogus + usuarios demo) solo en Development.
- **Compras:** `PurchaseService` concentra las reglas (función activa y futura, película en cartelera, 1 a 10 butacas sin repetir y dentro de `Rows` × `SeatsPerRow`, butacas libres, cargo vigente guardado en la compra; el cliente cancela las suyas hasta 2 h antes con `CancelMineAsync`, el staff cualquiera y siempre con `CancelAnyAsync`). Con entradas vendidas, una función no se borra ni cambia de horario, sala o película; una sala con funciones próximas no se da de baja (409). La carrera por una butaca la decide el índice único filtrado `(ShowtimeId, SeatRow, SeatNumber)` de `Tickets` → `SeatConflictException` (409). La hora sale de `TimeProvider` (hora local del cine, igual que `Showtime.StartTime`): nunca `DateTime.Now` en servicios; los tests usan `FixedTimeProvider`.
- **Tests de autorización:** `CinemAPI.Tests/WebApi/AuthorizationTests.cs` levanta la API con `WebApplicationFactory` (ambiente `Testing`, base LocalDB propia). Todo endpoint nuevo suma sus casos 401/403/permitido ahí.
- **Inyección de dependencias:** cada capa registra lo suyo: `Application/DependencyInjection.cs` (`AddApplication()`, servicios; el proyecto solo referencia `Microsoft.Extensions.DependencyInjection.Abstractions`) e `Infrastructure/DependencyInjection.cs` (`AddInfrastructure()`, contexto, repositorios y hasher); lo de la request y el JWT en `WebApi/Security` (`AddApiSecurity()`). `DependencyInjectionTests` valida que todo el grafo se resuelva.

## Regla de dependencias (innegociable)

```
WebApi ─────────► Application ──► Domain
Infrastructure ─► Application ──► Domain
```

- `Domain`: entidades, interfaces de repositorio, excepciones de dominio. No referencia a ningún otro proyecto ni a EF Core/ASP.NET.
- `Application`: servicios (casos de uso), DTOs, interfaces de servicio, mapeos. Solo referencia a `Domain`.
- `Infrastructure`: `CinemAPIContext`, configuración EF, repositorios, migraciones, seeder.
- `WebApi`: controllers delgados + composition root. Cero lógica de negocio.

## Definition of Done

Una tarea NO está terminada hasta que esto pase sin errores ni warnings nuevos:

```bash
dotnet build CinemAPI.slnx
dotnet test CinemAPI.slnx            # incluye integración contra LocalDB
dotnet format CinemAPI.slnx --verify-no-changes
```

Si se tocó el modelo de EF: crear migración con nombre descriptivo (`dotnet ef migrations add AddXxx -p Infrastructure -s WebApi`), revisarla, y avisar en el reporte. Nunca editar migraciones ya aplicadas.

## Cómo trabajar cada pedido

1. **Explorar:** leer el código relacionado y un ejemplo existente del mismo tipo (ej. para una entidad nueva, mirar `Genre`/`GenreService`/`GenreController`). Seguir el patrón del repo.
2. **Planificar:** si toca más de ~3 archivos, escribir el plan (archivo → capa → cambio) antes de implementar.
3. **Implementar** con las skills: `dotnet-csharp`, `clean-architecture`, `efcore-data`, `testing-dotnet`. Para un CRUD nuevo: `/nueva-entidad`.
4. **Verificar:** Definition of Done.
5. **Revisar:** para cambios medianos/grandes, pedirle al subagente `revisor-backend` que revise el diff.
6. **Reportar:** qué se hizo, qué se verificó, impacto en el front, pendientes.

## Contrato con el frontend

Cualquier cambio en rutas, verbos, nombres de campos JSON, códigos de estado o forma de los errores es un **cambio de contrato**:
- Preguntar antes de hacerlo.
- Si el repo del front está accesible (`../CinemAPIFront/cine-frontend` o agregado con `--add-dir`), leer su `CLAUDE.md` y actualizar `src/api/*.js` en el mismo trabajo.
- Si no está accesible, listar en el reporte exactamente qué debe cambiar en el front.

## Corrección de violaciones (regla del boy scout)

- Si en el código que estás tocando (o en el mismo archivo) hay una violación de estas reglas, **corregila en el mismo cambio** y mencionala en el reporte.
- Si está fuera del alcance del pedido, no la toques: anotala al final como **"Hallazgos fuera de alcance"** (archivo:línea · regla · corrección sugerida).
- Nunca cambiar comportamiento observable sin avisar. Cambios de contrato o de esquema de DB → preguntar primero.
- Para revisar y corregir todo el repo: `/auditar-backend`.

## Reglas de oro

- Controllers: reciben **Request DTOs** y devuelven **Response DTOs**. Nunca `[FromBody] Entidad` ni devolver entidades.
- Nada de `try/catch` en controllers para errores de negocio: las excepciones de dominio se traducen a `ProblemDetails` en un `IExceptionHandler` global.
- Toda operación de I/O es `async` y propaga `CancellationToken` desde el controller hasta EF. Nunca `.Result`, `.Wait()` ni `async void`.
- Lecturas con `AsNoTracking()`; filtros de fecha por rango (nunca `columna.Date == x`); nada de N+1.
- Nada de `ChangeTracker.Clear()` como parche: el servicio trae la entidad trackeada, la modifica y guarda.
- Nada de métodos `new` que ocultan los de `BaseRepository`: usar filtros globales (`HasQueryFilter`) o `virtual`/`override`.
- Configuración (cadena de conexión, orígenes CORS) en `appsettings`, no hardcodeada. Secretos fuera del repo (`dotnet user-secrets`).
- Registro de dependencias en `AddApplication()` / `AddInfrastructure()`, no todo en `Program.cs`.
- Todo cambio de comportamiento lleva test; todo bug corregido lleva un test que lo reproduce.
- No agregar paquetes NuGet sin decir por qué y sin preguntar.
- Git: trabajar en una rama (`feature/...`, `fix/...` o `refactor/...`), commits chicos con Conventional Commits (`feat(showtime): ...`). No hacer `git push` ni commitear en `main` sin pedido explícito.
