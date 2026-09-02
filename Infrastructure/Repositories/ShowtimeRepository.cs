using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// Re-implemento (con "new") GetAll/GetById/Add/Update/Delete de BaseRepository
// para filtrar por IsActive y hacer soft delete.
public class ShowtimeRepository : BaseRepository<Showtime>, IShowtimeRepository
{
    public ShowtimeRepository(CinemAPIContext context) : base(context)
    {
    }

    public new async Task<IEnumerable<Showtime>> GetAll()
    {
        return await _context.Showtimes
        .Where(s => s.IsActive)
        .OrderBy(s => s.StartTime)
        .ToListAsync();
    }

    // FirstOrDefaultAsync en vez de FindAsync (la base) porque necesito filtrar por
    // IsActive, no solo buscar por clave.
    public new async Task<Showtime?> GetById(Guid id)
    {
        return await _context.Showtimes
        .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
    }

    public new async Task<Showtime> Add(Showtime showtime)
    {
        _context.Showtimes.Add(showtime);
        await _context.SaveChangesAsync();
        return showtime;
    }

    public new async Task Update(Showtime showtime)
    {
        // Ver comentario equivalente en BaseRepository.Update / ScreenRepository.Update.
        _context.ChangeTracker.Clear();
        _context.Showtimes.Update(showtime);
        await _context.SaveChangesAsync();
    }

    // Igual que ShowtimeService.Delete (soft), por si alguien pega al repo directo.
    public new async Task Delete(Guid id)
    {
        var showtime = await GetById(id);
        if (showtime != null)
        {
            showtime.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId)
    {
        return await _context.Showtimes
        .Where(s => s.MovieId == movieId && s.IsActive)
        .OrderBy(s => s.StartTime)
        .ToListAsync();
    }

    // s.StartTime.Date == date.Date: compara solo el día, ignora la hora que venga en date.
    public async Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date)
    {
        return await _context.Showtimes
        .Where(s => s.ScreenId == screenId && s.IsActive && s.StartTime.Date == date.Date)
        .OrderBy(s => s.StartTime)
        .ToListAsync();
    }
}
