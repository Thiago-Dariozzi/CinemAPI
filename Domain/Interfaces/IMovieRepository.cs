using Domain.Entities;

namespace Domain.Interfaces;

public interface IMovieRepository : IBaseRepository<Movie>
{
    Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct);
    // En cartelera, de un género o de todos.
    Task<IReadOnlyList<Movie>> GetShowingMoviesAsync(Guid? genreId, CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct);
    Task<int> CountByGenreAsync(Guid genreId, CancellationToken ct);

    // Incluye películas dadas de baja: una función existente sigue ocupando la sala.
    Task<int?> GetDurationIncludingDeletedAsync(Guid movieId, CancellationToken ct);
}
