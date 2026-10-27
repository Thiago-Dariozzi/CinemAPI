using Application.DTOs.Auth;
using Application.DTOs.Users;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<UserResponse> GetCurrentUserAsync(CancellationToken ct);
    Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct);
    Task<bool> IsActiveUserAsync(Guid userId, CancellationToken ct);
}
