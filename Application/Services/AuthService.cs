using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IAuthService
{
    private const string EntityName = "el usuario";

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        await UserRules.EnsureEmailIsFreeAsync(userRepository, email, excludeId: null, ct);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = Roles.Client,
            IsActive = true,
        };
        userRepository.Add(user);
        await UserRules.SaveAsync(unitOfWork, ct);

        return CreateResponse(user);
    }

    // Usuario inexistente, dado de baja o contraseña incorrecta: el mismo 401, sin pistas.
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email.Trim(), ct);
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        return CreateResponse(user);
    }

    public async Task<UserResponse> GetCurrentUserAsync(CancellationToken ct) =>
        (await GetCurrentEntityAsync(ct)).ToResponse();

    public async Task UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await GetCurrentEntityAsync(ct);
        user.Name = request.Name.Trim();
        await unitOfWork.SaveChangesAsync(ct);
    }

    // 400 y no 401: el usuario sí está autenticado, solo se equivocó la contraseña actual (un 401
    // haría que el front cierre la sesión).
    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await GetCurrentEntityAsync(ct);
        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            throw new BusinessRuleException("La contraseña actual no es correcta.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<string?> GetActiveRoleAsync(Guid userId, CancellationToken ct) =>
        (await userRepository.GetByIdAsync(userId, ct))?.Role;

    private AuthResponse CreateResponse(User user)
    {
        var token = tokenGenerator.Create(user);
        return new AuthResponse(token.Token, token.ExpiresAt, user.ToResponse());
    }

    private async Task<User> GetCurrentEntityAsync(CancellationToken ct) =>
        await userRepository.GetByIdAsync(currentUser.Id, ct) ?? throw new NotFoundException(EntityName, currentUser.Id);
}
