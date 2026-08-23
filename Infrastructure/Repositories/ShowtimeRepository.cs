using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ShowtimeRepository : BaseRepository<Showtime>, IShowtimeRepository
{
    private readonly CinemAPIContext _context;
    public ShowtimeRepository(CinemAPIContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Showtime>> GetAll()
    {
        return await _context.Showtimes
        .Where(s => s.IsActive)
        .OrderBy(s => s.StartTime)
        .ToListAsync();
    }

    public async Task<Showtime?> GetById(Guid id)
    {
        return await _context.Showtimes
        .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
    }

    public async Task<Showtime> Add(Showtime showtime)
    {
        _context.Showtimes.Add(showtime);
        await _context.SaveChangesAsync();
        return showtime;
    }

    public async Task Update(Showtime showtime)
    {
        _context.Showtimes.Update(showtime);
        await _context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
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
}
