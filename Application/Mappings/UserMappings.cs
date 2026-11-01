using Application.DTOs.Users;
using Domain.Entities;

namespace Application.Mappings;

// Sin ToEntity: el alta arma el usuario en el servicio porque necesita el hash y el rol validado.
public static class UserMappings
{
    public static UserResponse ToResponse(this User user) => new(user.Id, user.Name, user.Email, user.Role, user.IsActive);

    public static void ApplyTo(this UpdateUserRequest request, User user)
    {
        user.Name = request.Name.Trim();
    }
}
