namespace Application.DTOs.Users;

/// <summary>
/// Representación pública de un usuario, sin el campo Password.
/// </summary>
public sealed record UserResponse(Guid Id, string Name, string Email, string Role, bool IsActive);
