namespace Application.DTOs;

public record UserResponseDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive
);
