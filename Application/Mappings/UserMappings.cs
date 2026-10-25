using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Mappings;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user) => new(user.Id, user.Name, user.Email, user.Role, user.IsActive);

    public static User ToEntity(this CreateUserRequest request) => new()
    {
        Name = request.Name,
        Email = request.Email,
        Password = request.Password,
        Role = string.IsNullOrWhiteSpace(request.Role) ? User.DefaultRole : request.Role,
    };

    public static void ApplyTo(this UpdateUserRequest request, User user)
    {
        user.Name = request.Name;
        user.Email = request.Email;
        user.Role = string.IsNullOrWhiteSpace(request.Role) ? User.DefaultRole : request.Role;
    }
}
