using Application.DTOs.Showtimes;

namespace Application.Interfaces;

public interface IShowtimeService
{
    Task<IReadOnlyList<ShowtimeResponse>> GetAllAsync(CancellationToken ct);
    Task<ShowtimeResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ShowtimeResponse>> GetByMovieIdAsync(Guid movieId, CancellationToken ct);
    Task<IReadOnlyList<ShowtimeResponse>> GetByScreenAndDateAsync(Guid screenId, DateOnly date, CancellationToken ct);
    Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateShowtimeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
