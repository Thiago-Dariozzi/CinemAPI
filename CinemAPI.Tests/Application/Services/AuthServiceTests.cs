using Application.DTOs.Auth;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class AuthServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly AuthService _sut;

    private readonly User _ana = new()
    {
        Id = Guid.NewGuid(),
        Name = "Ana",
        Email = "ana@cine.com",
        PasswordHash = "hash:secreta123",
        Role = Roles.Client,
    };

    public AuthServiceTests()
    {
        _users.Seed(_ana);
        _sut = new AuthService(_users, new FakePasswordHasher(), new FakeTokenGenerator(), _currentUser, _unitOfWork);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenAndUser()
    {
        var response = await _sut.LoginAsync(new LoginRequest("ANA@cine.com ", "secreta123"), Ct);

        Assert.Equal($"token-de-{_ana.Id}", response.Token);
        Assert.Equal(FakeTokenGenerator.ExpiresAt, response.ExpiresAt);
        Assert.Equal((_ana.Id, Roles.Client), (response.User.Id, response.User.Role));
    }

    [Theory]
    [InlineData("ana@cine.com", "incorrecta")]
    [InlineData("nadie@cine.com", "secreta123")]
    public async Task Login_WithWrongEmailOrPassword_ThrowsTheSameInvalidCredentialsException(string email, string password)
    {
        var ex = await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.LoginAsync(new LoginRequest(email, password), Ct));

        Assert.Equal("Email o contraseña incorrectos.", ex.Message);
    }

    [Fact]
    public async Task Login_WhenTheUserWasDeleted_ThrowsInvalidCredentialsException()
    {
        _ana.IsActive = false;

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _sut.LoginAsync(new LoginRequest(_ana.Email, "secreta123"), Ct));
    }

    [Fact]
    public async Task Register_AlwaysCreatesAClientWithAHashedPassword()
    {
        var response = await _sut.RegisterAsync(new RegisterRequest(" Beto ", "beto@cine.com", "12345678"), Ct);

        var stored = Assert.Single(_users.Stored, u => u.Id == response.User.Id);
        Assert.Equal((Roles.Client, "Beto", "hash:12345678"), (stored.Role, stored.Name, stored.PasswordHash));
        Assert.Equal($"token-de-{stored.Id}", response.Token);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Register_WithAnExistingEmail_ThrowsUserConflictException()
    {
        var ex = await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.RegisterAsync(new RegisterRequest("Otra", "Ana@Cine.com", "12345678"), Ct));

        Assert.Equal("Ya existe un usuario con ese email.", ex.Message);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // Dos registros simultáneos pasan el chequeo y el índice único decide: 409, no 500.
    [Fact]
    public async Task Register_WhenTheUniqueIndexRejectsTheEmail_ThrowsUserConflictException()
    {
        _unitOfWork.OnSave = () => throw new UniqueConstraintConflictException(new InvalidOperationException());

        await Assert.ThrowsAsync<UserConflictException>(
            () => _sut.RegisterAsync(new RegisterRequest("Beto", "beto@cine.com", "12345678"), Ct));
    }

    [Fact]
    public async Task ChangePassword_WithTheRightCurrentPassword_StoresTheNewHash()
    {
        _currentUser.As(_ana);

        await _sut.ChangePasswordAsync(new ChangePasswordRequest("secreta123", "nueva-clave"), Ct);

        Assert.Equal("hash:nueva-clave", _ana.PasswordHash);
    }

    // 400 y no 401: un 401 haría que el front cierre la sesión de alguien que solo se equivocó.
    [Fact]
    public async Task ChangePassword_WithAWrongCurrentPassword_ThrowsBusinessRuleException()
    {
        _currentUser.As(_ana);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.ChangePasswordAsync(new ChangePasswordRequest("otra", "nueva-clave"), Ct));
        Assert.Equal("hash:secreta123", _ana.PasswordHash);
    }

    [Fact]
    public async Task UpdateProfile_ChangesOnlyTheNameOfTheCurrentUser()
    {
        _currentUser.As(_ana);

        await _sut.UpdateProfileAsync(new UpdateProfileRequest(" Ana María "), Ct);

        Assert.Equal(("Ana María", "ana@cine.com", Roles.Client), (_ana.Name, _ana.Email, _ana.Role));
    }
}
