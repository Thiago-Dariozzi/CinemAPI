using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class UserServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly UserService _sut;

    private readonly User _ana = new() { Id = Guid.NewGuid(), Name = "Ana", Email = "ana@cine.com", Password = "x" };

    public UserServiceTests()
    {
        _users.Seed(_ana);
        _sut = new UserService(_users, _unitOfWork);
    }

    [Fact]
    public async Task Add_WhenEmailAlreadyExists_ThrowsUserConflictException()
    {
        var duplicate = new User { Name = "Otra Ana", Email = "ANA@cine.com", Password = "y" };

        var ex = await Assert.ThrowsAsync<UserConflictException>(() => _sut.AddAsync(duplicate, CancellationToken.None));
        Assert.Equal("Ya existe un usuario con ese email.", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Add_GeneratesIdActivatesAndSaves()
    {
        var created = await _sut.AddAsync(
            new User { Id = Guid.Empty, Name = "Beto", Email = "beto@cine.com", Password = "z", IsActive = false },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GetByEmail_WhenNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByEmailAsync("nadie@cine.com", CancellationToken.None));
    }
}
