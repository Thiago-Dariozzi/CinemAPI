using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class GenreRepository : BaseRepository<Genre>, IGenreRepository
{
    public GenreRepository(CinemAPIContext context) : base(context)
    {
    }

    public new async Task<IEnumerable<Genre>> GetAll()
    {
        return await _context.Genres
        .Where(g => g.IsActive)
        .ToListAsync();
    }

    public new async Task<Genre?> GetById(Guid id)
    {
        return await _context.Genres
        .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);
    }

    public new async Task<Genre> Add(Genre genre)
    {
        _context.Genres.Add(genre);
        await _context.SaveChangesAsync();
        return genre;
    }

    public new async Task Update(Genre genre)
    {
        // Ver comentario equivalente en BaseRepository.Update / ScreenRepository.Update.
        _context.ChangeTracker.Clear();
        _context.Genres.Update(genre);
        await _context.SaveChangesAsync();
    }

    public new async Task Delete(Guid id)
    {
        var genre = await GetById(id);
        if (genre != null)
        {
            genre.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
