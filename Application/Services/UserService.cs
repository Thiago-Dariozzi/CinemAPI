using System.Net.Http.Headers;
using Domain.Entities;
using Domain.Inerfaces;

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

}