using Application.DTOs.Users;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class UserServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly UserService _sut;

    private readonly User _ana = new() { Id = Guid.NewGuid(), Name = "Ana", Email = "ana@cine.com", Password = "secreta", Role = "Client" };

    public UserServiceTests()
    {
        _users.Seed(_ana);
        _sut = new UserService(_users, _unitOfWork);
    }

    [Fact]
    public async Task Create_WhenEmailAlreadyExists_ThrowsUserConflictException()
    {
        var ex = await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.CreateAsync(new CreateUserRequest("Otra Ana", "ANA@cine.com", "y", null), Ct));

        Assert.Equal("Ya existe un usuario con ese email.", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // Bug #2: el índice único incluye usuarios dados de baja; antes esto era un 500.
    [Fact]
    public async Task Create_WhenEmailBelongsToADeletedUser_ThrowsUserConflictException()
    {
        _ana.IsActive = false;

        await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.CreateAsync(new CreateUserRequest("Nueva", "ana@cine.com", "y", null), Ct));
    }

    [Fact]
    public async Task Update_ToAnotherUsersEmail_ThrowsUserConflictException()
    {
        var beto = new User { Id = Guid.NewGuid(), Name = "Beto", Email = "beto@cine.com", Password = "z" };
        _users.Seed(beto);

        await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.UpdateAsync(beto.Id, new UpdateUserRequest("Beto", "ana@cine.com", null), Ct));
    }

    [Fact]
    public async Task Create_GeneratesIdActivatesAndDefaultsRoleToClient()
    {
        var created = await _sut.CreateAsync(new CreateUserRequest("Beto", "beto@cine.com", "z", null), Ct);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActive);
        Assert.Equal("Client", created.Role);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    // Antes el PUT recibía la entidad entera y pisaba la contraseña (#1 y #6 de la auditoría).
    [Fact]
    public async Task Update_ChangesProfileButKeepsPasswordAndActiveState()
    {
        await _sut.UpdateAsync(_ana.Id, new UpdateUserRequest("Ana María", "anam@cine.com", "Admin"), Ct);

        Assert.Equal(("Ana María", "anam@cine.com", "Admin"), (_ana.Name, _ana.Email, _ana.Role));
        Assert.Equal("secreta", _ana.Password);
        Assert.True(_ana.IsActive);
    }

    [Fact]
    public async Task GetByEmail_WhenNotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByEmailAsync("nadie@cine.com", Ct));
    }
}
