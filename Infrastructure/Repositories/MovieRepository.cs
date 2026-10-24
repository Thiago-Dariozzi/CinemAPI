using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MovieRepository(CinemAPIContext context) : BaseRepository<Movie>(context), IMovieRepository
{
    public async Task<IEnumerable<Movie>> GetActiveMovies() =>
        await Context.Movies.AsNoTracking()
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();

    public async Task<IEnumerable<Movie>> GetByGenre(Guid genreId) =>
        await Context.Movies.AsNoTracking()
            .Where(m => m.GenreId == genreId)
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();

    public Task<int> CountByGenre(Guid genreId) =>
        Context.Movies.CountAsync(m => m.GenreId == genreId);

    // Sin ToLower(): la columna usa la collation por defecto de la base (case-insensitive),
    // así que la comparación ya ignora mayúsculas y puede usar índices.
    public async Task<IEnumerable<Movie>> GetByTitle(string title) =>
        await Context.Movies.AsNoTracking()
            .Where(m => m.Title == title)
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();
}
