using System;
using Domain.Entities;

namespace Domain.Inerfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User>? GetByEmail(string email);
}