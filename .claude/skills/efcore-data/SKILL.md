---
name: efcore-data
description: Buenas prácticas de Entity Framework Core 10 y repositorios en CinemAPI. Usar al tocar CinemAPIContext, repositorios, consultas LINQ a la base, soft delete, configuraciones de entidades, migraciones o el DbSeeder.
---

# EF Core y repositorios

## Configuración de entidades

Una clase por entidad en `Infrastructure/Configurations`, y el contexto las aplica todas:

```csharp
public sealed class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Price).HasPrecision(10, 2);
        builder.HasOne<Movie>().WithMany().HasForeignKey(s => s.MovieId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Screen>().WithMany().HasForeignKey(s => s.ScreenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.ScreenId, s.StartTime });   // consultas de solapamiento
        builder.HasQueryFilter(s => s.IsActive);                  // soft delete global
    }
}

// CinemAPIContext.OnModelCreating
modelBuilder.ApplyConfigurationsFromAssembly(typeof(CinemAPIContext).Assembly);
```

- Mover la configuración actual de `OnModelCreating` a estas clases **sin cambiar el modelo** (la migración generada después debe salir vacía; si no, revisar).
- Conservar lo que ya está bien: `HasPrecision(10, 2)` en dinero, `Restrict` en FKs, índice único filtrado de `Genre.Name` con collation `Modern_Spanish_CI_AI`.
- `decimal` siempre con precisión explícita. Strings con `HasMaxLength`.

## Soft delete con filtro global

- `HasQueryFilter(e => e.IsActive)` en cada entidad con soft delete. Así `GetAll`/`GetById` de `BaseRepository` ya filtran y desaparecen los métodos `new` duplicados en cada repositorio.
- Cuando haga falta ver inactivos (ej. administración), usar `IgnoreQueryFilters()` de forma explícita y comentada.
- EF avisa (warning 10622) cuando una entidad filtrada es el lado requerido de una relación (ej. `Genre` → `Movie`). Es esperable con soft delete: aplicar el filtro también en la entidad dependiente y revisar que no se oculten filas por error.
- Ojo con `FindAsync`: no aplica query filters si la entidad ya está trackeada; preferir `FirstOrDefaultAsync(e => e.Id == id, ct)`.

## Repositorio base sin `new`

```csharp
public abstract class BaseRepository<T>(CinemAPIContext context) : IBaseRepository<T> where T : class, IEntity
{
    protected CinemAPIContext Context { get; } = context;

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct) =>
        await Context.Set<T>().AsNoTracking().ToListAsync(ct);

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Context.Set<T>().FirstOrDefaultAsync(e => e.Id == id, ct);   // trackeada: se usa para modificar

    public void Add(T entity) => Context.Set<T>().Add(entity);
}
```

- Si un repositorio necesita otro orden u otro filtro, `override` (no `new`).
- `IEntity` (en Domain) expone `Guid Id` e `IsActive` para poder escribir código genérico.

## Guardar cambios: un solo lugar, sin `ChangeTracker.Clear()`

El patrón correcto para Update:

```csharp
// Servicio
var showtime = await _showtimes.GetByIdAsync(id, ct) ?? throw new NotFoundException("Función", id);
showtime.Reschedule(request.StartTime!.Value, request.Price);   // o asignar propiedades si aún no hay métodos
await ValidateNoOverlapAsync(showtime, ct);
await _unitOfWork.SaveChangesAsync(ct);
```

- La entidad viene trackeada desde el repositorio, se modifica y se guarda. No hay segunda instancia, así que no hace falta `ChangeTracker.Clear()` ni `Update(entity)`.
- `IUnitOfWork` (interfaz en Application, implementada por el contexto o un wrapper en Infrastructure) con `SaveChangesAsync`. Los repositorios no llaman `SaveChangesAsync` por su cuenta: así un caso de uso que toca varias entidades se guarda en una sola transacción.
- Delete lógico: `entity.IsActive = false` (o `entity.Deactivate()`) + `SaveChangesAsync`. Un solo lugar implementa el soft delete.

## Consultas

| ❌ Evitar | ✅ Hacer |
|---|---|
| `.Where(s => s.StartTime.Date == date.Date)` (no usa índices) | `.Where(s => s.StartTime >= day && s.StartTime < day.AddDays(1))` |
| Traer todo y filtrar en memoria (`GetAll()` + `FirstOrDefault`) | Filtrar en la consulta (`AnyAsync`, `Where`) |
| Lecturas trackeadas | `AsNoTracking()` en consultas de solo lectura |
| Llamar a la base dentro de un `foreach` (N+1) | Una consulta con `Join`/`Include` o `Where(x => ids.Contains(x.Id))` |
| `Count() > 0` | `AnyAsync(ct)` |
| Devolver entidades completas para listados | Proyectar con `Select` a lo necesario |

Ejemplo — solapamiento de funciones en una sola consulta:

```csharp
public async Task<IReadOnlyList<(Showtime Showtime, int DurationMinutes)>> GetByScreenAndDayWithDurationAsync(
    Guid screenId, DateOnly day, CancellationToken ct)
{
    var start = day.ToDateTime(TimeOnly.MinValue);
    var end = start.AddDays(1);
    var rows = await Context.Showtimes.AsNoTracking()
        .Where(s => s.ScreenId == screenId && s.StartTime >= start && s.StartTime < end)
        .Join(Context.Movies, s => s.MovieId, m => m.Id, (s, m) => new { s, m.DurationMinutes })
        .OrderBy(x => x.s.StartTime)
        .ToListAsync(ct);
    return rows.Select(x => (x.s, x.DurationMinutes)).ToList();
}
```

Duplicados de `Genre`: la columna ya tiene collation accent/case-insensitive, así que la comparación puede hacerse en la base (`AnyAsync(g => g.Name == name && g.Id != excludeId, ct)`) en lugar de traer todos los géneros.

## Interfaces de repositorio

- Viven en `Domain/Interfaces` (convención del repo). Métodos con intención y `Async` + `CancellationToken`.
- No exponer `IQueryable` fuera de Infrastructure.

## Migraciones

- Nombre descriptivo en PascalCase: `AddShowtimeScreenStartTimeIndex`.
- `dotnet ef migrations add <Nombre> -p Infrastructure -s WebApi`.
- Revisar el código generado antes de aplicar; nunca editar ni borrar migraciones ya aplicadas/compartidas.
- Un cambio de modelo = una migración en el mismo commit.
- Cambiar tipos/columnas existentes (ej. pasar `Ticket` a referenciar `ShowtimeId`) es un cambio de esquema: preguntar antes.

## Seeder

- Solo en Development. Idempotente (ya chequea si hay películas).
- Si se agregan filtros globales o reglas nuevas, verificar que el seeder siga generando datos válidos.
