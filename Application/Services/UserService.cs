using Application.DTOs.Users;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// ABM de usuarios para el staff. Admin ve y administra clientes; SuperAdmin además administra Admins
// y ve a los SuperAdmins. Lo que no puede administrar responde 403.
public sealed class UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IUserService
{
    private const string EntityName = "el usuario";

    // Incluye a los dados de baja (IsActive = false) para que se puedan reactivar.
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(string? role, string? search, CancellationToken ct)
    {
        var visible = UserRules.VisibleRoles(currentUser.Role);
        if (role is not null)
        {
            EnsureRoleExists(role);
            if (!visible.Contains(role))
            {
                throw new ForbiddenException($"No tenés permiso para ver usuarios con el rol {role}.");
            }
        }

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var users = await userRepository.GetByRolesAsync(role is null ? visible : [role], term, ct);
        return users.Select(u => u.ToResponse()).ToList();
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await GetEntityAsync(id, ct);
        if (!UserRules.VisibleRoles(currentUser.Role).Contains(user.Role))
        {
            throw new ForbiddenException("No tenés permiso para ver este usuario.");
        }

        return user.ToResponse();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var role = string.IsNullOrWhiteSpace(request.Role) ? Roles.Client : request.Role;
        EnsureCanAssign(role);
        var email = request.Email.Trim();
        await UserRules.EnsureEmailIsFreeAsync(userRepository, email, excludeId: null, ct);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true,
        };
        userRepository.Add(user);
        await UserRules.SaveAsync(unitOfWork, ct);
        return user.ToResponse();
    }

    // La contraseña no se toca desde acá: UpdateUserRequest no la incluye (cada uno cambia la suya).
    public async Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await GetEntityAsync(id, ct);
        EnsureCanManage(user);

        var role = string.IsNullOrWhiteSpace(request.Role) ? user.Role : request.Role;
        EnsureCanAssign(role);
        var email = request.Email.Trim();
        await UserRules.EnsureEmailIsFreeAsync(userRepository, email, excludeId: id, ct);

        request.ApplyTo(user);
        user.Email = email;
        user.Role = role;
        await UserRules.SaveAsync(unitOfWork, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await GetEntityAsync(id, ct);
        if (user.Id == currentUser.Id)
        {
            throw new ForbiddenException("No podés darte de baja a vos mismo.");
        }

        if (user.Role == Roles.SuperAdmin)
        {
            EnsureActorIsSuperAdmin("Solo un SuperAdmin puede dar de baja a otro SuperAdmin.");
            await EnsureNotTheLastSuperAdminAsync(ct);
        }
        else
        {
            EnsureCanManage(user);
        }

        user.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Las mismas reglas que la baja: Admin reactiva clientes; SuperAdmin, cualquiera.
    public async Task ReactivateAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.GetByIdIncludingInactiveAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
        if (user.Role == Roles.SuperAdmin)
        {
            EnsureActorIsSuperAdmin("Solo un SuperAdmin puede reactivar a otro SuperAdmin.");
        }
        else
        {
            EnsureCanManage(user);
        }

        if (user.IsActive)
        {
            throw new UserConflictException("El usuario ya está activo.");
        }

        user.IsActive = true;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private void EnsureActorIsSuperAdmin(string message)
    {
        if (currentUser.Role != Roles.SuperAdmin)
        {
            throw new ForbiddenException(message);
        }
    }

    private async Task EnsureNotTheLastSuperAdminAsync(CancellationToken ct)
    {
        if (await userRepository.CountByRoleAsync(Roles.SuperAdmin, ct) <= 1)
        {
            throw new ForbiddenException("No se puede dar de baja al último SuperAdmin activo.");
        }
    }

    private void EnsureCanManage(User user)
    {
        if (!UserRules.ManageableRoles(currentUser.Role).Contains(user.Role))
        {
            throw new ForbiddenException($"No tenés permiso para modificar usuarios con el rol {user.Role}.");
        }
    }

    private void EnsureCanAssign(string role)
    {
        EnsureRoleExists(role);
        if (!UserRules.ManageableRoles(currentUser.Role).Contains(role))
        {
            throw new ForbiddenException(role == Roles.SuperAdmin
                ? "No se pueden crear SuperAdmins desde la API."
                : $"No tenés permiso para asignar el rol {role}.");
        }
    }

    private static void EnsureRoleExists(string role)
    {
        if (role is not (Roles.SuperAdmin or Roles.Admin or Roles.Client))
        {
            throw new BusinessRuleException($"El rol '{role}' no existe. Usá Client, Admin o SuperAdmin.");
        }
    }

    private async Task<User> GetEntityAsync(Guid id, CancellationToken ct) =>
        await userRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
}
