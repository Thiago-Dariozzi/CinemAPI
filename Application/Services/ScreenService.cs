using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class ScreenService(IScreenRepository screenRepository, IUnitOfWork unitOfWork)
{
    public async Task<IEnumerable<Screen>> GetAll()
    {
        return await screenRepository.GetAll();
    }

    public async Task<Screen?> GetById(Guid id)
    {
        return await screenRepository.GetById(id);
    }

    public async Task<Screen> Add(Screen screen)
    {
        screen.Id = Guid.NewGuid();
        screen.IsActive = true;
        screenRepository.Add(screen);
        await unitOfWork.SaveChangesAsync();
        return screen;
    }

    public async Task Update(Screen screen)
    {
        var existing = await screenRepository.GetById(screen.Id);
        if (existing == null)
        {
            return;
        }

        existing.Name = screen.Name;
        existing.Capacity = screen.Capacity;
        existing.IsActive = screen.IsActive;
        await unitOfWork.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var screen = await screenRepository.GetById(id);
        if (screen != null)
        {
            screen.IsActive = false;
            await unitOfWork.SaveChangesAsync();
        }
    }
}
