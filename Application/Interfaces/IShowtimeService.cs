using Domain.Entities;

namespace Application.Interfaces;

public interface IShowtimeService
{
    Task<IReadOnlyList<Showtime>> GetAllAsync(CancellationToken ct);
    Task<Showtime> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct);
    Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct);
    Task<Showtime> AddAsync(Showtime showtime, CancellationToken ct);
    Task UpdateAsync(Showtime showtime, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
