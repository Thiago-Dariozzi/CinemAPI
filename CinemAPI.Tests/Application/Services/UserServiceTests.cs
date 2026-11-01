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
    private readonly FakeCurrentUser _currentUser = new();
    private readonly UserService _sut;

    private readonly User _superAdmin = NewUser("Sara", Roles.SuperAdmin);
    private readonly User _admin = NewUser("Alan", Roles.Admin);
    private readonly User _ana = NewUser("Ana", Roles.Client);

    public UserServiceTests()
    {
        _users.Seed(_superAdmin, _admin, _ana);
        _currentUser.As(_admin);
        _sut = new UserService(_users, new FakePasswordHasher(), _currentUser, _unitOfWork);
    }

    [Fact]
    public async Task Create_WhenEmailAlreadyExists_ThrowsUserConflictException()
    {
        var ex = await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.CreateAsync(new CreateUserRequest("Otra Ana", "ANA@cine.com", "12345678", null), Ct));

        Assert.Equal("Ya existe un usuario con ese email.", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // Bug #2: el índice único incluye usuarios dados de baja; antes esto era un 500.
    [Fact]
    public async Task Create_WhenEmailBelongsToADeletedUser_ThrowsUserConflictException()
    {
        _ana.IsActive = false;

        await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.CreateAsync(new CreateUserRequest("Nueva", "ana@cine.com", "12345678", null), Ct));
    }

    [Fact]
    public async Task Create_HashesThePasswordAndDefaultsRoleToClient()
    {
        var created = await _sut.CreateAsync(new CreateUserRequest("Beto", "beto@cine.com", "12345678", null), Ct);

        var stored = Assert.Single(_users.Stored, u => u.Id == created.Id);
        Assert.Equal("hash:12345678", stored.PasswordHash);
        Assert.Equal(Roles.Client, created.Role);
        Assert.True(created.IsActive);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.SuperAdmin)]
    public async Task Create_AsAdmin_WithAStaffRole_ThrowsForbiddenException(string role)
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.CreateAsync(new CreateUserRequest("Nuevo", "nuevo@cine.com", "12345678", role), Ct));
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_AsSuperAdmin_CanCreateAdmins_ButNotSuperAdmins()
    {
        _currentUser.As(_superAdmin);

        var admin = await _sut.CreateAsync(new CreateUserRequest("Nuevo", "nuevo@cine.com", "12345678", Roles.Admin), Ct);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.CreateAsync(new CreateUserRequest("Otro", "otro@cine.com", "12345678", Roles.SuperAdmin), Ct));

        Assert.Equal(Roles.Admin, admin.Role);
        Assert.Equal("No se pueden crear SuperAdmins desde la API.", ex.Message);
    }

    [Fact]
    public async Task Create_WithAnUnknownRole_ThrowsBusinessRuleException()
    {
        _currentUser.As(_superAdmin);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(new CreateUserRequest("Nuevo", "nuevo@cine.com", "12345678", "Root"), Ct));
    }

    [Fact]
    public async Task GetAll_AsAdmin_OnlyReturnsClients()
    {
        var users = await _sut.GetAllAsync(role: null, search: null, Ct);

        Assert.Equal([_ana.Id], users.Select(u => u.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetAllAsync(Roles.Admin, search: null, Ct));
    }

    [Fact]
    public async Task GetAll_IncludesDeletedUsers_SoTheyCanBeReactivated()
    {
        var deleted = NewUser("Beto", Roles.Client);
        deleted.IsActive = false;
        _users.Seed(deleted);

        var users = await _sut.GetAllAsync(Roles.Client, search: null, Ct);

        Assert.Equal([(_ana.Id, true), (deleted.Id, false)], users.Select(u => (u.Id, u.IsActive)));
    }

    [Theory]
    [InlineData("an", "Ana")]      // parte del nombre, sin distinguir mayúsculas
    [InlineData("BETO@", "Beto")]  // parte del email
    [InlineData("  ana  ", "Ana")] // se ignoran los espacios de los costados
    public async Task GetAll_SearchesByNameOrEmail(string search, string expected)
    {
        _users.Seed(NewUser("Beto", Roles.Client));

        var users = await _sut.GetAllAsync(Roles.Client, search, Ct);

        Assert.Equal([expected], users.Select(u => u.Name));
    }

    [Fact]
    public async Task GetAll_WithABlankSearch_ReturnsEveryone()
    {
        _users.Seed(NewUser("Beto", Roles.Client));

        Assert.Equal(2, (await _sut.GetAllAsync(Roles.Client, "   ", Ct)).Count);
    }

    [Fact]
    public async Task Reactivate_AsAdmin_AClient_TurnsItActiveAgain()
    {
        _ana.IsActive = false;

        await _sut.ReactivateAsync(_ana.Id, Ct);

        Assert.True(_ana.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Reactivate_AsAdmin_AnAdmin_ThrowsForbiddenException()
    {
        var otherAdmin = NewUser("Otro", Roles.Admin);
        otherAdmin.IsActive = false;
        _users.Seed(otherAdmin);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.ReactivateAsync(otherAdmin.Id, Ct));
        Assert.False(otherAdmin.IsActive);
    }

    [Fact]
    public async Task Reactivate_AsSuperAdmin_AnAdminOrASuperAdmin_Works()
    {
        _currentUser.As(_superAdmin);
        var formerAdmin = NewUser("Otro", Roles.Admin);
        var formerSuperAdmin = NewUser("Ex", Roles.SuperAdmin);
        formerAdmin.IsActive = formerSuperAdmin.IsActive = false;
        _users.Seed(formerAdmin, formerSuperAdmin);

        await _sut.ReactivateAsync(formerAdmin.Id, Ct);
        await _sut.ReactivateAsync(formerSuperAdmin.Id, Ct);

        Assert.True(formerAdmin.IsActive && formerSuperAdmin.IsActive);
    }

    [Fact]
    public async Task Reactivate_AsAdmin_ASuperAdmin_ThrowsForbiddenException()
    {
        var formerSuperAdmin = NewUser("Ex", Roles.SuperAdmin);
        formerSuperAdmin.IsActive = false;
        _users.Seed(formerSuperAdmin);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.ReactivateAsync(formerSuperAdmin.Id, Ct));
        Assert.False(formerSuperAdmin.IsActive);
    }

    [Fact]
    public async Task GetAll_AsSuperAdmin_WithoutRole_SearchesAcrossEveryVisibleRole()
    {
        _currentUser.As(_superAdmin);

        var users = await _sut.GetAllAsync(role: null, search: "A", Ct);

        Assert.Equal(["Alan", "Ana", "Sara"], users.Select(u => u.Name));
        Assert.Equal(["Ana"], (await _sut.GetAllAsync(Roles.Client, "A", Ct)).Select(u => u.Name));
    }

    [Fact]
    public async Task Reactivate_AnActiveUser_ThrowsUserConflictException()
    {
        var ex = await Assert.ThrowsAsync<UserConflictException>(() => _sut.ReactivateAsync(_ana.Id, Ct));

        Assert.Equal("El usuario ya está activo.", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Reactivate_AUserThatDoesNotExist_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ReactivateAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task Delete_AsSuperAdmin_AnotherSuperAdmin_WorksWhileAnotherOneRemains()
    {
        var other = NewUser("Otra", Roles.SuperAdmin);
        _users.Seed(other);
        _currentUser.As(_superAdmin);

        await _sut.DeleteAsync(other.Id, Ct);

        Assert.False(other.IsActive);
    }

    [Fact]
    public async Task Delete_AsSuperAdmin_AnAdmin_SoftDeletes()
    {
        _currentUser.As(_superAdmin);

        await _sut.DeleteAsync(_admin.Id, Ct);

        Assert.False(_admin.IsActive);
    }

    [Fact]
    public async Task Update_AsSuperAdmin_AnotherSuperAdmin_ThrowsForbiddenException()
    {
        var other = NewUser("Otra", Roles.SuperAdmin);
        _users.Seed(other);
        _currentUser.As(_superAdmin);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateAsync(other.Id, new UpdateUserRequest("Otra", other.Email, Roles.Admin), Ct));
        Assert.Equal(Roles.SuperAdmin, other.Role);
    }

    [Fact]
    public async Task GetById_AsAdmin_AnotherAdmin_ThrowsForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetByIdAsync(_superAdmin.Id, Ct));
    }

    [Fact]
    public async Task Update_ToAnotherUsersEmail_ThrowsUserConflictException()
    {
        var beto = NewUser("Beto", Roles.Client);
        _users.Seed(beto);

        await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.UpdateAsync(beto.Id, new UpdateUserRequest("Beto", _ana.Email, null), Ct));
    }

    // Antes el PUT recibía la entidad entera y pisaba la contraseña (#1 y #6 de la auditoría).
    [Fact]
    public async Task Update_ChangesProfileButKeepsPasswordRoleAndActiveState()
    {
        await _sut.UpdateAsync(_ana.Id, new UpdateUserRequest("Ana María", "anam@cine.com", null), Ct);

        Assert.Equal(("Ana María", "anam@cine.com", Roles.Client), (_ana.Name, _ana.Email, _ana.Role));
        Assert.Equal("hash:secreta", _ana.PasswordHash);
        Assert.True(_ana.IsActive);
    }

    [Fact]
    public async Task Update_AsAdmin_PromotingAClientToAdmin_ThrowsForbiddenException()
    {
        await Assert.ThrowsAsync<ForbiddenException>(
            () => _sut.UpdateAsync(_ana.Id, new UpdateUserRequest("Ana", _ana.Email, Roles.Admin), Ct));
        Assert.Equal(Roles.Client, _ana.Role);
    }

    [Fact]
    public async Task Delete_AsAdmin_ADifferentAdmin_ThrowsForbiddenException()
    {
        var otherAdmin = NewUser("Otro", Roles.Admin);
        _users.Seed(otherAdmin);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteAsync(otherAdmin.Id, Ct));
        Assert.True(otherAdmin.IsActive);
    }

    [Fact]
    public async Task Delete_Themselves_ThrowsForbiddenException()
    {
        _currentUser.As(_superAdmin);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteAsync(_superAdmin.Id, Ct));
        Assert.True(_superAdmin.IsActive);
    }

    // Defensa en profundidad: aunque hoy no se pueda borrar a uno mismo, nunca debe quedar el sistema
    // sin SuperAdmin (por ejemplo, si el que borra fue dado de baja después de loguearse).
    [Fact]
    public async Task Delete_TheLastActiveSuperAdmin_ThrowsForbiddenException()
    {
        var formerSuperAdmin = NewUser("Ex", Roles.SuperAdmin);
        formerSuperAdmin.IsActive = false;
        _users.Seed(formerSuperAdmin);
        _currentUser.As(formerSuperAdmin);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => _sut.DeleteAsync(_superAdmin.Id, Ct));

        Assert.Equal("No se puede dar de baja al último SuperAdmin activo.", ex.Message);
        Assert.True(_superAdmin.IsActive);
    }

    [Fact]
    public async Task Delete_AsAdmin_AClient_SoftDeletes()
    {
        await _sut.DeleteAsync(_ana.Id, Ct);

        Assert.False(_ana.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    private static User NewUser(string name, string role) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = $"{name.ToLowerInvariant()}@cine.com",
        PasswordHash = "hash:secreta",
        Role = role,
    };
}
