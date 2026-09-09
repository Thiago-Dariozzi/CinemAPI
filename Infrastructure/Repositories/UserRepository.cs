using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(CinemAPIContext context) : base(context)
    {
    }

    public new async Task<IEnumerable<User>> GetAll()
    {
        return await _context.Users
        .Where(u => u.IsActive)
        .ToListAsync();
    }

    public new async Task<User?> GetById(Guid id)
    {
        return await _context.Users
        .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
    }

    public async Task<User?> GetByEmail(string email)
    {
        return await _context.Users
        .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
    }

}