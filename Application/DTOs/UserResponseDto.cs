namespace Application.DTOs;

/// <summary>
/// Representación pública de un usuario, sin el campo Password.
/// </summary>
public record UserResponseDto(Guid Id, string Name, string Email, string Role, bool IsActive);
