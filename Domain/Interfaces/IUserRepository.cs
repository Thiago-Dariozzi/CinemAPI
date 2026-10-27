using Domain.Entities;

namespace Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    // Incluye usuarios dados de baja: el índice único de Email también los cuenta.
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct);

    Task<IReadOnlyList<User>> GetByRolesAsync(IReadOnlyCollection<string> roles, CancellationToken ct);

    // Solo activos (filtro global).
    Task<int> CountByRoleAsync(string role, CancellationToken ct);
}
