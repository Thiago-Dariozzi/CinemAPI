using Domain.Entities;

namespace Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    // Incluye usuarios dados de baja: el índice único de Email también los cuenta.
    Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct);

    // Incluye a los dados de baja, para poder reactivarlos. search: parte del nombre o del email (null = todos).
    Task<IReadOnlyList<User>> GetByRolesAsync(IReadOnlyCollection<string> roles, string? search, CancellationToken ct);

    // Trackeado e incluyendo a los dados de baja: lo usa la reactivación.
    Task<User?> GetByIdIncludingInactiveAsync(Guid id, CancellationToken ct);

    // Solo activos (filtro global).
    Task<int> CountByRoleAsync(string role, CancellationToken ct);
}
