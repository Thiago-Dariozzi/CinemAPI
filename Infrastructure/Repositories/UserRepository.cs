using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class UserRepository(CinemAPIContext context) : BaseRepository<User>(context), IUserRepository
{
    // Sin ToLower(): la columna usa la collation por defecto de la base (case-insensitive).
    public Task<User?> GetByEmail(string email) =>
        Context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
}
