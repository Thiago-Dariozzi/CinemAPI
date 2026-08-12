using Domain.Entities;
using Domain.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MovieRepository : BaseRepository<Movie>, IMovieRepository
{
    private readonly CinemAPIContext _context;
    public MovieRepository(CinemAPIContext context) : base(context)
    {
        _context = context;
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