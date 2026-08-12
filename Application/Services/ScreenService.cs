using System;
using Domain.Entities;
using Domain.Interfaces;


namespace Application.Services;

public class ScreenService
{
    private readonly IScreenRepository _repo;

    public ScreenService(IScreenRepository screenRepository)
    {
        _repo = screenRepository;
    }
    public async Task<IEnumerable<Screen>> GetAll()
    {
        return await _repo.GetAll();
    }
    public async Task<Screen?> GetById(Guid id)
    {
        return await _repo.GetById(id);
    }
    public async Task<Screen> Add(Screen screen)
    {
        screen.Id = Guid.NewGuid();
        screen.IsActive = true;
        return await _repo.Add(screen);
    }
    public async Task Update(Screen screen)
    {
        await _repo.Update(screen);
    }
    public async Task Delete(Guid id)
    {
        var screen = await _repo.GetById(id);
        if (screen != null)
        {
            screen.IsActive = false;
            await _repo.Update(screen);
        }
    }
}