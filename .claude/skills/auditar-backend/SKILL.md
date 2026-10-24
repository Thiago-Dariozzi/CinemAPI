---
name: auditar-backend
description: Audita CinemAPI contra las buenas prácticas del proyecto (.NET, Clean Architecture, EF Core, tests), genera un reporte con severidades y corrige por lotes verificando build y tests. Invocar con /auditar-backend [ruta o tema opcional].
disable-model-invocation: true
argument-hint: "[ruta | controllers | services | repositories | domain | tests]"
---

# /auditar-backend

Alcance: `$ARGUMENTS` (si está vacío, todo el repo).

## 1. Línea de base (no tocar código todavía)

1. `git status` limpio; si no, avisar y parar. Crear rama `refactor/buenas-practicas` si estás en `main` (agregarle `-<fecha>` solo si esa rama ya existe).
2. Correr y guardar el resultado de: `dotnet build CinemAPI.slnx`, `dotnet test CinemAPI.slnx` (si existe), `dotnet format CinemAPI.slnx --verify-no-changes`. Contar warnings.
3. Leer `CLAUDE.md`, `docs/hallazgos-iniciales.md` (si existe) y las skills `clean-architecture`, `dotnet-csharp`, `efcore-data`, `testing-dotnet`.

## 2. Revisión

Recorrer cada archivo del alcance y chequear, como mínimo:

- **Capas:** referencias entre proyectos; usings prohibidos (`Microsoft.EntityFrameworkCore` fuera de Infrastructure, `Microsoft.AspNetCore` fuera de WebApi); lógica de negocio en controllers o repositorios.
- **Contrato API:** `[FromBody] Entidad`, entidades devueltas por controllers, `Password` expuesto, retornos sin tipar, query params obligatorios sin validar.
- **Errores:** `try/catch` en controllers, excepciones mal usadas (conflicto vs no encontrado), ausencia de handler global / ProblemDetails.
- **Async:** falta de `CancellationToken`, `.Result`/`.Wait()`, nombres sin `Async`.
- **EF Core:** `ChangeTracker.Clear()`, métodos `new` que ocultan la base, consultas no sargables (`.Date ==`), falta de `AsNoTracking`, N+1, carga de tablas completas para filtrar en memoria, `SaveChangesAsync` disperso en repositorios.
- **Configuración:** valores hardcodeados (CORS, URLs), seeder fuera de Development, DI en `Program.cs`.
- **Estilo C#:** nombres, `using` innecesarios, constantes en MAYÚSCULAS, tipos no `sealed`, typos en nombres de archivo/tipo, nullability silenciada con `!`.
- **Tests:** existencia de `CinemAPI.Tests`, tests de arquitectura, cobertura de reglas de negocio.

Usar `Grep` para cada patrón en todo el alcance, no solo los archivos que ya se conocen.

## 3. Reporte

Escribir/actualizar `docs/auditoria-backend.md`:

| # | Severidad | Archivo:línea | Regla | Problema | Corrección propuesta | ¿Cambia contrato/esquema? |
|---|---|---|---|---|---|---|

Severidades: **Crítica** (seguridad, pérdida de datos, bug), **Alta** (viola la arquitectura), **Media** (mantenibilidad/rendimiento), **Baja** (estilo).

Agrupar en lotes (uno por categoría) en este orden sugerido:
1. Red de seguridad: crear `CinemAPI.Tests` + tests de caracterización de las reglas actuales (solapamiento, géneros) + tests de arquitectura.
2. Estilo y configuración sin impacto (usings, nombres, `sealed`, `.editorconfig`, CORS a appsettings, seeder solo en Development, DI en extensiones).
3. EF Core interno (configuraciones separadas, query filters, sacar `new` y `ChangeTracker.Clear()`, consultas por rango, `AsNoTracking`, N+1, unit of work).
4. Async + `CancellationToken` de punta a punta.
5. Interfaces de servicio + controllers delgados.
6. DTOs de request/response + validación + handler global con ProblemDetails (**cambio de contrato**).
7. Dominio más rico (métodos en entidades) — solo con confirmación.

Mostrar el resumen (cantidad por severidad y lotes) y **esperar confirmación** antes de corregir, salvo que el usuario haya pedido corregir directamente.

## 4. Corrección por lotes

Por cada lote:
1. Aplicar los cambios del lote, nada más.
2. `dotnet build` + `dotnet test` + `dotnet format --verify-no-changes` en verde. Si algo se rompe, arreglarlo dentro del lote.
3. Si el lote cambió el modelo de EF: generar migración y verificar que solo contenga lo esperado (una migración vacía es la señal correcta en refactors de configuración).
4. Commit `refactor(<área>): <qué>` (o `test:` / `chore:`).
5. Marcar los ítems como ✅ en el reporte.

Reglas durante la corrección:
- No cambiar comportamiento observable. Si un hallazgo lo requiere (contrato, esquema de DB, mensajes que el front interpreta), dejarlo como "Requiere decisión" y preguntar.
- Si el front está accesible, actualizar `src/api/*.js` en el mismo lote que cambia el contrato; si no, listar los cambios necesarios.
- No agregar paquetes NuGet sin preguntar.

## 5. Cierre

Resumen final: lotes aplicados, commits, hallazgos pendientes con motivo, cambios que necesita el front, y próximos pasos sugeridos.
