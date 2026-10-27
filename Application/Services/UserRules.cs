using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// Reglas de usuarios que comparten el registro público (AuthService) y el ABM del staff (UserService).
internal static class UserRules
{
    private const string DuplicateEmailMessage = "Ya existe un usuario con ese email.";

    // Cuenta también a los usuarios dados de baja: el índice único de Email los incluye, y sin
    // este chequeo el alta terminaba en un 500 por violación del índice.
    public static async Task EnsureEmailIsFreeAsync(IUserRepository users, string email, Guid? excludeId, CancellationToken ct)
    {
        if (await users.EmailExistsAsync(email, excludeId, ct))
        {
            throw new UserConflictException(DuplicateEmailMessage);
        }
    }

    // Dos altas simultáneas con el mismo email pueden pasar el chequeo anterior: el índice único
    // decide y se responde el mismo 409.
    public static async Task SaveAsync(IUnitOfWork unitOfWork, CancellationToken ct)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintConflictException)
        {
            throw new UserConflictException(DuplicateEmailMessage);
        }
    }

    // Qué roles puede ver y administrar cada rol del staff. Nadie crea ni edita SuperAdmins por la
    // API: el inicial sale del seed.
    public static IReadOnlyList<string> VisibleRoles(string actorRole) => actorRole switch
    {
        Roles.SuperAdmin => [Roles.SuperAdmin, Roles.Admin, Roles.Client],
        Roles.Admin => [Roles.Client],
        _ => [],
    };

    public static IReadOnlyList<string> ManageableRoles(string actorRole) => actorRole switch
    {
        Roles.SuperAdmin => [Roles.Admin, Roles.Client],
        Roles.Admin => [Roles.Client],
        _ => [],
    };
}
