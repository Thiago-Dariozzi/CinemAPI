using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;

namespace CinemAPI.Tests.Fakes;

public sealed class FakeGenreRepository : InMemoryRepository<Genre>, IGenreRepository;

public sealed class FakeMovieRepository : InMemoryRepository<Movie>, IMovieRepository
{
    public Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct) =>
        List(Active().Where(m => string.Equals(m.Title, title, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Movie>> GetActiveMoviesAsync(CancellationToken ct) =>
        List(Active().OrderByDescending(m => m.ReleaseDate));

    public Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct) =>
        List(Active().Where(m => m.GenreId == genreId));

    public Task<int> CountByGenreAsync(Guid genreId, CancellationToken ct) =>
        Task.FromResult(Active().Count(m => m.GenreId == genreId));

    private static Task<IReadOnlyList<Movie>> List(IEnumerable<Movie> movies) =>
        Task.FromResult<IReadOnlyList<Movie>>(movies.ToList());
}

// Necesita las películas para armar la ocupación, igual que el join del repositorio real.
public sealed class FakeShowtimeRepository(FakeMovieRepository movies) : InMemoryRepository<Showtime>, IShowtimeRepository
{
    public Task<IReadOnlyList<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Showtime>>(Active().Where(s => s.MovieId == movieId).OrderBy(s => s.StartTime).ToList());

    public Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Showtime>>(ByScreenAndDay(screenId, date).ToList());

    public async Task<IReadOnlyList<ShowtimeOccupancy>> GetOccupancyByScreenAndDateAsync(
        Guid screenId, DateTime date, CancellationToken ct)
    {
        var activeMovies = (await movies.GetAllAsync(ct)).ToDictionary(m => m.Id);
        return ByScreenAndDay(screenId, date)
            .Select(s => activeMovies.TryGetValue(s.MovieId, out var m)
                ? new ShowtimeOccupancy(s.Id, s.StartTime, m.DurationMinutes, m.Title)
                : new ShowtimeOccupancy(s.Id, s.StartTime, null, null))
            .ToList();
    }

    private IEnumerable<Showtime> ByScreenAndDay(Guid screenId, DateTime date) =>
        Active()
            .Where(s => s.ScreenId == screenId && s.StartTime >= date.Date && s.StartTime < date.Date.AddDays(1))
            .OrderBy(s => s.StartTime);
}
