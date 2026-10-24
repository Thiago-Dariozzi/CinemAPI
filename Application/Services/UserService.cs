using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class UserService(IUserRepository userRepository, IUnitOfWork unitOfWork) : IUserService
{
    private const string EntityName = "el usuario";

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct) => userRepository.GetAllAsync(ct);

    public async Task<User> GetByIdAsync(Guid id, CancellationToken ct) =>
        await userRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    public async Task<User> GetByEmailAsync(string email, CancellationToken ct) =>
        await userRepository.GetByEmailAsync(email, ct) ?? throw new NotFoundException(EntityName, email);

    public async Task<User> AddAsync(User user, CancellationToken ct)
    {
        if (await userRepository.GetByEmailAsync(user.Email, ct) != null)
        {
            throw new UserConflictException("Ya existe un usuario con ese email.");
        }

        user.Id = Guid.NewGuid();
        user.IsActive = true;
        // Sin hash todavía (no vimos autenticación en la materia): se guarda tal cual llega.
        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        var existing = await GetByIdAsync(user.Id, ct);

        existing.Name = user.Name;
        existing.Email = user.Email;
        existing.Password = user.Password;
        existing.Role = user.Role;
        existing.IsActive = user.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await GetByIdAsync(id, ct);
        user.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
