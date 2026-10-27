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
    // Rol actual del usuario, o null si no existe o fue dado de baja. Se compara con el del token en
    // cada request: un cambio de rol o una baja cortan el acceso sin esperar a que el token venza.
    Task<string?> GetActiveRoleAsync(Guid userId, CancellationToken ct);
}
