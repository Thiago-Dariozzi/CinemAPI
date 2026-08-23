using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MovieRepository : BaseRepository<Movie>, IMovieRepository
{
    public MovieRepository(CinemAPIContext context) : base(context)
    {
    }

    public new async Task<IEnumerable<Movie>> GetAll()
    {
        return await _context.Movies
        .Where(m => m.IsActive)
        .ToListAsync();
    }

    public new async Task<Movie?> GetById(Guid id)
    {
        return await _context.Movies
        .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
    }

    public async Task<IEnumerable<Movie>> GetActiveMovies()
    {
        return await _context.Movies
        .Where(m => m.IsActive)
        .OrderByDescending(m => m.ReleaseDate)
        .ToListAsync();
    }

    public async Task<IEnumerable<Movie>> GetByGenre(string genre)
    {
        return await _context.Movies
        .Where(m => m.Genre.ToLower() == genre.ToLower() && m.IsActive)
        .OrderByDescending(m => m.ReleaseDate)
        .ToListAsync();
    }

    public async Task<IEnumerable<Movie>> GetByTitle(string title)
    {
        return await _context.Movies
        .Where(m => m.Title.ToLower() == title.ToLower() && m.IsActive)
        .OrderByDescending(m => m.ReleaseDate)
        .ToListAsync();
    }

}