using System;
using Domain.Entities;
using Domain.Interfaces;

namespace Domain.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User>? GetByEmail(string email);
}