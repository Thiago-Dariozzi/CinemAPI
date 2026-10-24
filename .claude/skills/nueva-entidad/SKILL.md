---
name: nueva-entidad
description: Crea un CRUD completo para una entidad nueva en CinemAPI siguiendo la arquitectura del proyecto (entidad, configuración EF, repositorio, servicio, DTOs, controller, migración y tests). Usar cuando se pida agregar una entidad, recurso o ABM nuevo, o invocar con /nueva-entidad <Nombre> <campos>.
argument-hint: "<NombreEnSingular> <campo:tipo ...>  (ej. Fee name:string percentageRate:decimal)"
---

# /nueva-entidad

Pedido: `$ARGUMENTS`

Antes de escribir, leer la implementación de referencia más parecida (`Genre` para entidades simples, `Showtime` si tiene reglas y FKs) y respetar las skills `clean-architecture`, `dotnet-csharp`, `efcore-data`, `testing-dotnet`. Si la referencia todavía tiene violaciones conocidas (entidades en controllers, `new` en repositorios, etc.), **no copiarlas**: seguir el patrón correcto de las skills.

## Pasos (en orden, verificando build al final de cada capa)

1. **Domain**
   - `Domain/Entities/<Nombre>.cs`: `Guid Id`, propiedades, `bool IsActive = true`. Implementa `IEntity` si existe.
   - Reglas propias de la entidad como métodos (ej. `Deactivate()`).
   - `Domain/Interfaces/I<Nombre>Repository.cs : IBaseRepository<<Nombre>>` con solo los métodos extra necesarios.
   - Excepción de dominio si hay una regla de negocio nueva.
2. **Infrastructure**
   - `Configurations/<Nombre>Configuration.cs`: clave, longitudes, precisión de decimales, FKs con `Restrict`, índices, `HasQueryFilter(e => e.IsActive)`.
   - `DbSet<<Nombre>>` en `CinemAPIContext`.
   - `Repositories/<Nombre>Repository.cs : BaseRepository<<Nombre>>, I<Nombre>Repository`.
   - Registrar en `AddInfrastructure`.
   - Migración `Add<Nombre>Entity` y revisarla.
3. **Application**
   - `DTOs/<Plural>/Create<Nombre>Request.cs`, `Update<Nombre>Request.cs` (DataAnnotations), `<Nombre>Response.cs`.
   - `Mappings/<Nombre>Mappings.cs` con `ToResponse()`.
   - `Interfaces/I<Nombre>Service.cs` y `Services/<Nombre>Service.cs` (Id generado en el servicio, `NotFoundException`, soft delete, `CancellationToken`).
   - Registrar en `AddApplication`.
4. **WebApi**
   - `Controllers/<Nombre>Controller.cs` delgado: GET (todos), GET `{id:guid}`, POST (201 + `CreatedAtAction`), PUT `{id:guid}` (204), DELETE `{id:guid}` (204). `ActionResult<T>` y `ProducesResponseType`.
5. **Tests**
   - `CinemAPI.Tests/Application/Services/<Nombre>ServiceTests.cs`: alta, no encontrado, soft delete y cada regla de negocio.
6. **Verificar** Definition of Done (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`).
7. **Front:** si el repo del front está accesible, ofrecer `/nueva-pantalla-crud <Nombre>` allí; si no, listar endpoints y forma del JSON para que se implemente.

## Reporte

Lista de archivos creados por capa, endpoints con ejemplo de request/response, migración creada, tests agregados.
