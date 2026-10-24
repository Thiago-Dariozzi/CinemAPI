# CLAUDE.md — CinemAPI (backend)

Sistema de gestión para un complejo de cines. Proyecto académico: el alcance lo marca la materia.
Este archivo se carga en cada sesión. Las reglas detalladas viven en `.claude/skills/` y se cargan solas cuando el tema lo requiere.

## Stack real del repo

| Tema | Valor |
|---|---|
| Runtime | .NET 10 (`net10.0`), C# con `Nullable` e `ImplicitUsings` activados |
| Solución | `CinemAPI.slnx` |
| Proyectos | `Domain`, `Application`, `Infrastructure`, `WebApi` (+ `CinemAPI.Tests` cuando exista) |
| Base de datos | SQL Server LocalDB vía EF Core 10 (`CinemAPIContext`), migraciones en `Infrastructure/Migrations` |
| Datos de prueba | `Infrastructure/Data/DbSeeder.cs` con Bogus |
| API | Controllers (`[ApiController]`, `api/[controller]`), OpenAPI nativo (`AddOpenApi`) |
| Casos de uso | Clases de servicio en `Application/Services` (NO usamos MediatR/CQRS) |
| Persistencia | Repositorios: interfaces en `Domain/Interfaces`, implementación en `Infrastructure/Repositories` |
| Errores de negocio | Excepciones de dominio en `Domain/Exceptions` (ej. `ShowtimeConflictException` → 409) |
| DTOs | `Application/DTOs` (hoy solo `UserResponseDto`; ver skill `clean-architecture`) |
| Soft delete | Todas las entidades usan `IsActive`; borrar = `IsActive = false` |
| Frontend que consume la API | repo `CinemAPIFront` (React + Vite), CORS para `http://localhost:5173` |

## Decisiones tomadas (no cambiar sin pedido explícito)

- Se mantiene el estilo **Services + Repositories + Controllers**. No introducir MediatR, CQRS, AutoMapper, FluentValidation ni otras librerías "de arquitectura": se usan las herramientas que trae .NET (DataAnnotations, `IExceptionHandler`, `ProblemDetails`, mapeo manual).
- Autenticación: **todavía no se vio en la materia**. No agregar JWT/Identity/hash de contraseñas por iniciativa propia; está registrado como deuda conocida (ver `docs/hallazgos-iniciales.md`).
- Los Ids son `Guid` generados en el servicio, nunca confiados del cliente.
- Soft delete con `IsActive` en todas las entidades.
- Identificadores (clases, métodos, variables, rutas) en **inglés**. Mensajes al usuario en **español**.
- Comentarios: solo para explicar el **porqué** (no el qué). <!-- PENDIENTE confirmar con el profe: idioma de los comentarios (en el front se pidió quitar los comentarios en español). Hasta confirmarlo: no borrar comentarios existentes en masa. -->

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
dotnet test CinemAPI.slnx            # cuando exista el proyecto de tests
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
- Si el repo del front está accesible (`../CinemAPIFront` o agregado con `--add-dir`), leer su `CLAUDE.md` y actualizar `src/api/*.js` en el mismo trabajo.
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
