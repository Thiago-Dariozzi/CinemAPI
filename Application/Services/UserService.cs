using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public class UserService
{
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }
    public async Task<User?> GetByEmail(string email)
    {
        return  await _repo.GetByEmail(email);
    }
    public async Task<IEnumerable<User>> GetAll()
    {
        return await _repo.GetAll();
    }


    public async Task<User?> GetById(Guid id)
    {
        return await _repo.GetById(id);
    }
    
    public async Task<User?> Add(User user)
    {
        user.Id = Guid.NewGuid();
        user.IsActive = true;
        // Sin hash todavía (no vimos autenticación en la materia): se guarda tal cual llega.
        return await _repo.Add(user);
    }

    public async Task Update(User user)
    {
        await _repo.Update(user);
    }
    
    public async Task Delete(Guid id)
    {
        var user = await _repo.GetById(id);
        if (user != null)
        {
            user.IsActive = false;
            await _repo.Update(user);
        }
    }

}