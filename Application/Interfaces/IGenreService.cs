using Application.DTOs.Genres;

namespace Application.Interfaces;

public interface IGenreService
{
    Task<IReadOnlyList<GenreResponse>> GetAllAsync(CancellationToken ct);
    Task<GenreResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<GenreResponse> CreateAsync(CreateGenreRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateGenreRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
