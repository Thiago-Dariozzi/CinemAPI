using Domain.Entities;

namespace Application.Interfaces;

public interface IMovieService
{
    Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetActiveMoviesAsync(CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct);
    Task<Movie> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Movie> AddAsync(Movie movie, CancellationToken ct);
    Task UpdateAsync(Movie movie, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
