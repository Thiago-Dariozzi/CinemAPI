using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace Infrastructure.Repositories;

public class ScreenRepository : BaseRepository<Screen>, IScreenRepository
{
    private readonly CinemAPIContext _context;
    public ScreenRepository(CinemAPIContext context) : base(context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Screen>> GetAll()
    {
        return await _context.Screens
        .Where(s => s.IsActive)
        .ToListAsync();
    }

    public async Task<Screen?> GetById(Guid id)
    {
        return await _context.Screens
        .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
    }

    public async Task<Screen> Add(Screen screen)
    {
        _context.Screens.Add(screen);
        await _context.SaveChangesAsync();
        return screen;
    }

    public async Task Update(Screen screen)
    {
        _context.Screens.Update(screen);
        await _context.SaveChangesAsync();
    }
    
    public async Task Delete(Guid id)
    {
        var screen = await GetById(id);
        if (screen != null)
        {
            screen.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

}
