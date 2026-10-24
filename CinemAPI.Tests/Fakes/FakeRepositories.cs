using Domain.Entities;
using Domain.Interfaces;

namespace CinemAPI.Tests.Fakes;

public sealed class FakeGenreRepository() : InMemoryRepository<Genre>(g => g.Id, g => g.IsActive), IGenreRepository;

public sealed class FakeMovieRepository() : InMemoryRepository<Movie>(m => m.Id, m => m.IsActive), IMovieRepository
{
    public Task<IEnumerable<Movie>> GetByTitle(string title) =>
        Task.FromResult<IEnumerable<Movie>>(Active().Where(m => string.Equals(m.Title, title, StringComparison.OrdinalIgnoreCase)).ToList());

    public Task<IEnumerable<Movie>> GetActiveMovies() =>
        Task.FromResult<IEnumerable<Movie>>(Active().OrderByDescending(m => m.ReleaseDate).ToList());

    public Task<IEnumerable<Movie>> GetByGenre(Guid genreId) =>
        Task.FromResult<IEnumerable<Movie>>(Active().Where(m => m.GenreId == genreId).ToList());
}

public sealed class FakeShowtimeRepository() : InMemoryRepository<Showtime>(s => s.Id, s => s.IsActive), IShowtimeRepository
{
    public Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId) =>
        Task.FromResult<IEnumerable<Showtime>>(Active().Where(s => s.MovieId == movieId).OrderBy(s => s.StartTime).ToList());

    public Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date) =>
        Task.FromResult<IEnumerable<Showtime>>(Active()
            .Where(s => s.ScreenId == screenId && s.StartTime.Date == date.Date)
            .OrderBy(s => s.StartTime)
            .ToList());
}
