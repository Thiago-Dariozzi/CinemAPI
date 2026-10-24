using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class MovieRepository(CinemAPIContext context) : BaseRepository<Movie>(context), IMovieRepository
{
    public async Task<IReadOnlyList<Movie>> GetActiveMoviesAsync(CancellationToken ct) =>
        await Context.Movies.AsNoTracking()
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct) =>
        await Context.Movies.AsNoTracking()
            .Where(m => m.GenreId == genreId)
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync(ct);

    public Task<int> CountByGenreAsync(Guid genreId, CancellationToken ct) =>
        Context.Movies.CountAsync(m => m.GenreId == genreId, ct);

    // Sin ToLower(): la columna usa la collation por defecto de la base (case-insensitive),
    // así que la comparación ya ignora mayúsculas y puede usar índices.
    public async Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct) =>
        await Context.Movies.AsNoTracking()
            .Where(m => m.Title == title)
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync(ct);
}
