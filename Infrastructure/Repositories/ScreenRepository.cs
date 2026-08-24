using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace Infrastructure.Repositories;

public class ScreenRepository : BaseRepository<Screen>, IScreenRepository
{
    public ScreenRepository(CinemAPIContext context) : base(context)
    {
    }
    public new async Task<IEnumerable<Screen>> GetAll()
    {
        return await _context.Screens
        .Where(s => s.IsActive)
        .ToListAsync();
    }

    public new async Task<Screen?> GetById(Guid id)
    {
        return await _context.Screens
        .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
    }

    public new async Task<Screen> Add(Screen screen)
    {
        _context.Screens.Add(screen);
        await _context.SaveChangesAsync();
        return screen;
    }

    public new async Task Update(Screen screen)
    {
        // Ver comentario equivalente en BaseRepository.Update: evita el conflicto de
        // tracking con la instancia que dejó trackeada el GetById del controller.
        _context.ChangeTracker.Clear();
        _context.Screens.Update(screen);
        await _context.SaveChangesAsync();
    }

    public new async Task Delete(Guid id)
    {
        var screen = await GetById(id);
        if (screen != null)
        {
            screen.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

}
