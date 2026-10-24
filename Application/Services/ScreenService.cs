using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class ScreenService(IScreenRepository screenRepository, IUnitOfWork unitOfWork) : IScreenService
{
    private const string EntityName = "la sala";

    public Task<IReadOnlyList<Screen>> GetAllAsync(CancellationToken ct) => screenRepository.GetAllAsync(ct);

    public async Task<Screen> GetByIdAsync(Guid id, CancellationToken ct) =>
        await screenRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    public async Task<Screen> AddAsync(Screen screen, CancellationToken ct)
    {
        screen.Id = Guid.NewGuid();
        screen.IsActive = true;
        screenRepository.Add(screen);
        await unitOfWork.SaveChangesAsync(ct);
        return screen;
    }

    public async Task UpdateAsync(Screen screen, CancellationToken ct)
    {
        var existing = await GetByIdAsync(screen.Id, ct);

        existing.Name = screen.Name;
        existing.Capacity = screen.Capacity;
        existing.IsActive = screen.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var screen = await GetByIdAsync(id, ct);
        screen.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
