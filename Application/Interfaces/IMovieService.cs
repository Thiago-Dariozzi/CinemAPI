using Application.DTOs.Movies;

namespace Application.Interfaces;

public interface IMovieService
{
    Task<IReadOnlyList<MovieResponse>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<MovieResponse>> GetShowingMoviesAsync(CancellationToken ct);
    Task<IReadOnlyList<MovieResponse>> GetByGenreAsync(Guid genreId, CancellationToken ct);
    Task<IReadOnlyList<MovieResponse>> GetByTitleAsync(string title, CancellationToken ct);
    Task<MovieResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<MovieResponse> CreateAsync(CreateMovieRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateMovieRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
