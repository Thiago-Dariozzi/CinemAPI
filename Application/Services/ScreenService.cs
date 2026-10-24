using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class ScreenService(IScreenRepository screenRepository, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<Screen>> GetAllAsync(CancellationToken ct)
    {
        return await screenRepository.GetAllAsync(ct);
    }

    public async Task<Screen?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await screenRepository.GetByIdAsync(id, ct);
    }

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
        var existing = await screenRepository.GetByIdAsync(screen.Id, ct);
        if (existing == null)
        {
            return;
        }

        existing.Name = screen.Name;
        existing.Capacity = screen.Capacity;
        existing.IsActive = screen.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var screen = await screenRepository.GetByIdAsync(id, ct);
        if (screen != null)
        {
            screen.IsActive = false;
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
