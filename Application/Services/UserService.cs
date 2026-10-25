using Application.DTOs.Users;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class UserService(IUserRepository userRepository, IUnitOfWork unitOfWork) : IUserService
{
    private const string EntityName = "el usuario";

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct) =>
        (await userRepository.GetAllAsync(ct)).Select(u => u.ToResponse()).ToList();

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<UserResponse> GetByEmailAsync(string email, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(email, ct) ?? throw new NotFoundException(EntityName, email);
        return user.ToResponse();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        await EnsureEmailIsFreeAsync(request.Email, excludeId: null, ct);

        var user = request.ToEntity();
        user.Id = Guid.NewGuid();
        user.IsActive = true;
        // Sin hash todavía (no vimos autenticación en la materia): se guarda tal cual llega.
        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(ct);
        return user.ToResponse();
    }

    // La contraseña no se toca desde acá: UpdateUserRequest no la incluye.
    public async Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await GetEntityAsync(id, ct);
        await EnsureEmailIsFreeAsync(request.Email, excludeId: id, ct);

        request.ApplyTo(user);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await GetEntityAsync(id, ct);
        user.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Cuenta también a los usuarios dados de baja: el índice único de Email los incluye, y sin
    // este chequeo el alta terminaba en un 500 por violación del índice.
    private async Task EnsureEmailIsFreeAsync(string email, Guid? excludeId, CancellationToken ct)
    {
        if (await userRepository.EmailExistsAsync(email, excludeId, ct))
        {
            throw new UserConflictException("Ya existe un usuario con ese email.");
        }
    }

    private async Task<User> GetEntityAsync(Guid id, CancellationToken ct) =>
        await userRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
}
