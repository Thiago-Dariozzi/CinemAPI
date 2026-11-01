using Application.DTOs.Users;

namespace Application.DTOs.Auth;

public sealed record AuthResponse(string Token, DateTime ExpiresAt, UserResponse User);
