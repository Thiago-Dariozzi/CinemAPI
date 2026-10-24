using Domain.Entities;

namespace Domain.Interfaces;

public interface IMovieRepository : IBaseRepository<Movie>
{
    Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetActiveMoviesAsync(CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct);
    Task<int> CountByGenreAsync(Guid genreId, CancellationToken ct);
}
