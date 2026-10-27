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

    public async Task<IReadOnlyList<User>> GetByRolesAsync(IReadOnlyCollection<string> roles, CancellationToken ct) =>
        await Context.Users.AsNoTracking().Where(u => roles.Contains(u.Role)).OrderBy(u => u.Name).ToListAsync(ct);

    public Task<int> CountByRoleAsync(string role, CancellationToken ct) =>
        Context.Users.CountAsync(u => u.Role == role, ct);
}
