using Application.DTOs.Screens;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class ScreenService(IScreenRepository screenRepository, IUnitOfWork unitOfWork) : IScreenService
{
    private const string EntityName = "la sala";

    public async Task<IReadOnlyList<ScreenResponse>> GetAllAsync(CancellationToken ct) =>
        (await screenRepository.GetAllAsync(ct)).Select(s => s.ToResponse()).ToList();

    public async Task<ScreenResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<ScreenResponse> CreateAsync(CreateScreenRequest request, CancellationToken ct)
    {
        var screen = request.ToEntity();
        screen.Id = Guid.NewGuid();
        screen.IsActive = true;
        screenRepository.Add(screen);
        await unitOfWork.SaveChangesAsync(ct);
        return screen.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateScreenRequest request, CancellationToken ct)
    {
        var screen = await GetEntityAsync(id, ct);
        request.ApplyTo(screen);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var screen = await GetEntityAsync(id, ct);
        screen.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Screen> GetEntityAsync(Guid id, CancellationToken ct) =>
        await screenRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);
}
