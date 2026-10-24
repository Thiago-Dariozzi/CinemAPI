using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
{
    public async Task<User?> GetByEmail(string email)
    {
        return await userRepository.GetByEmail(email);
    }

    public async Task<IEnumerable<User>> GetAll()
    {
        return await userRepository.GetAll();
    }

    public async Task<User?> GetById(Guid id)
    {
        return await userRepository.GetById(id);
    }

    public async Task<User> Add(User user)
    {
        user.Id = Guid.NewGuid();
        user.IsActive = true;
        // Sin hash todavía (no vimos autenticación en la materia): se guarda tal cual llega.
        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync();
        return user;
    }

    public async Task Update(User user)
    {
        var existing = await userRepository.GetById(user.Id);
        if (existing == null)
        {
            return;
        }

        existing.Name = user.Name;
        existing.Email = user.Email;
        existing.Password = user.Password;
        existing.Role = user.Role;
        existing.IsActive = user.IsActive;
        await unitOfWork.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var user = await userRepository.GetById(id);
        if (user != null)
        {
            user.IsActive = false;
            await unitOfWork.SaveChangesAsync();
        }
    }
}
