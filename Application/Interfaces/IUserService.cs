using Application.DTOs.Users;

namespace Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(string? role, string? search, CancellationToken ct);
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task ReactivateAsync(Guid id, CancellationToken ct);
}
