using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    private readonly CinemAPIContext _context;
    public UserRepository(CinemAPIContext context) : base(context)
    {
    }
    public async Task<User>? GetByEmail(string email)
    {
        return  await _context.Set<User>().FindAsync(email);
    }

}