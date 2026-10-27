using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserRepository(CinemAPIContext context) : BaseRepository<User>(context), IUserRepository
{
    // Sin ToLower(): la columna usa la collation por defecto de la base (case-insensitive).
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        Context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    // IgnoreQueryFilters a propósito: el índice único de Email incluye a los usuarios dados de baja.
    public Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct) =>
        Context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && u.Id != excludeId, ct);

    // IgnoreQueryFilters a propósito: el staff ve a los dados de baja para poder reactivarlos.
    // Contains se traduce a LIKE, que con la collation de la base no distingue mayúsculas.
    public async Task<IReadOnlyList<User>> GetByRolesAsync(IReadOnlyCollection<string> roles, string? search, CancellationToken ct) =>
        await Context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(u => roles.Contains(u.Role))
            .Where(u => search == null || u.Name.Contains(search) || u.Email.Contains(search))
            .OrderBy(u => u.Name)
            .ToListAsync(ct);

    // IgnoreQueryFilters a propósito: reactivar es justamente buscar a un usuario dado de baja.
    public Task<User?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken ct) =>
        Context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<int> CountByRoleAsync(string role, CancellationToken ct) =>
        Context.Users.CountAsync(u => u.Role == role, ct);
}
