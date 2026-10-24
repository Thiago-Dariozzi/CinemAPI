using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        return await userRepository.GetByEmailAsync(email, ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct)
    {
        return await userRepository.GetAllAsync(ct);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await userRepository.GetByIdAsync(id, ct);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct)
    {
        user.Id = Guid.NewGuid();
        user.IsActive = true;
        // Sin hash todavía (no vimos autenticación en la materia): se guarda tal cual llega.
        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        var existing = await userRepository.GetByIdAsync(user.Id, ct);
        if (existing == null)
        {
            return;
        }

        existing.Name = user.Name;
        existing.Email = user.Email;
        existing.Password = user.Password;
        existing.Role = user.Role;
        existing.IsActive = user.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(id, ct);
        if (user != null)
        {
            user.IsActive = false;
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
