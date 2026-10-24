using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;

namespace CinemAPI.Tests.Fakes;

public sealed class FakeGenreRepository : InMemoryRepository<Genre>, IGenreRepository;

public sealed class FakeMovieRepository : InMemoryRepository<Movie>, IMovieRepository
{
    public Task<IEnumerable<Movie>> GetByTitle(string title) =>
        Task.FromResult<IEnumerable<Movie>>(Active().Where(m => string.Equals(m.Title, title, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task<IEnumerable<Movie>> GetActiveMovies() =>
        Task.FromResult<IEnumerable<Movie>>(Active().OrderByDescending(m => m.ReleaseDate).ToList());

    public Task<IEnumerable<Movie>> GetByGenre(Guid genreId) =>
        Task.FromResult<IEnumerable<Movie>>(Active().Where(m => m.GenreId == genreId).ToList());

    public Task<int> CountByGenre(Guid genreId) => Task.FromResult(Active().Count(m => m.GenreId == genreId));
}

// Necesita las películas para armar la ocupación, igual que el join del repositorio real.
public sealed class FakeShowtimeRepository(FakeMovieRepository movies) : InMemoryRepository<Showtime>, IShowtimeRepository
{
    public Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId) =>
        Task.FromResult<IEnumerable<Showtime>>(Active().Where(s => s.MovieId == movieId).OrderBy(s => s.StartTime).ToList());

    public Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date) =>
        Task.FromResult<IEnumerable<Showtime>>(ByScreenAndDay(screenId, date).ToList());

    public async Task<IEnumerable<ShowtimeOccupancy>> GetOccupancyByScreenAndDate(Guid screenId, DateTime date)
    {
        var activeMovies = (await movies.GetAll()).ToDictionary(m => m.Id);
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
