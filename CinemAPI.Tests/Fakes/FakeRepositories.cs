using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;

namespace CinemAPI.Tests.Fakes;

public sealed class FakeGenreRepository : InMemoryRepository<Genre>, IGenreRepository;

public sealed class FakeScreenRepository : InMemoryRepository<Screen>, IScreenRepository;

public sealed class FakeUserRepository : InMemoryRepository<User>, IUserRepository
{
    // Igual que la collation de la base: sin distinguir mayúsculas.
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        Task.FromResult(Active().FirstOrDefault(u => SameEmail(u.Email, email)));

    // Como el repositorio real: cuenta también a los dados de baja.
    public Task<bool> EmailExistsAsync(string email, Guid? excludeId, CancellationToken ct) =>
        Task.FromResult(Stored.Any(u => SameEmail(u.Email, email) && u.Id != excludeId));

    public Task<IReadOnlyList<User>> GetByRolesAsync(IReadOnlyCollection<string> roles, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<User>>(Active().Where(u => roles.Contains(u.Role)).OrderBy(u => u.Name).ToList());

    public Task<int> CountByRoleAsync(string role, CancellationToken ct) =>
        Task.FromResult(Active().Count(u => u.Role == role));

    private static bool SameEmail(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

public sealed class FakeTicketRepository : InMemoryRepository<Ticket>, ITicketRepository
{
    public Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Ticket>>(Active().Where(t => t.UserId == userId).ToList());
}

public sealed class FakeMovieRepository : InMemoryRepository<Movie>, IMovieRepository
{
    public Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct) =>
        List(Active().Where(m => string.Equals(m.Title, title, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Movie>> GetShowingMoviesAsync(CancellationToken ct) =>
        List(Active().Where(m => m.IsShowing).OrderByDescending(m => m.ReleaseDate));

    public Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct) =>
        List(Active().Where(m => m.GenreId == genreId));

    public Task<int> CountByGenreAsync(Guid genreId, CancellationToken ct) =>
        Task.FromResult(Active().Count(m => m.GenreId == genreId));

    public Task<int?> GetDurationIncludingDeletedAsync(Guid movieId, CancellationToken ct) =>
        Task.FromResult(Stored.Where(m => m.Id == movieId).Select(m => (int?)m.DurationMinutes).FirstOrDefault());

    private static Task<IReadOnlyList<Movie>> List(IEnumerable<Movie> movies) =>
        Task.FromResult<IReadOnlyList<Movie>>(movies.ToList());
}

// Necesita las películas para armar la ocupación, igual que el join del repositorio real.
public sealed class FakeShowtimeRepository(FakeMovieRepository movies) : InMemoryRepository<Showtime>, IShowtimeRepository
{
    public Task<IReadOnlyList<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Showtime>>(Active().Where(s => s.MovieId == movieId).OrderBy(s => s.StartTime).ToList());

    public Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Showtime>>(Active()
            .Where(s => s.ScreenId == screenId && s.StartTime >= date.Date && s.StartTime < date.Date.AddDays(1))
            .OrderBy(s => s.StartTime)
            .ToList());

    // Como el repositorio real: la duración sale aunque la película esté dada de baja.
    public Task<IReadOnlyList<ShowtimeOccupancy>> GetOccupancyByScreenAsync(
        Guid screenId, DateTime from, DateTime to, CancellationToken ct)
    {
        var allMovies = movies.Stored.ToDictionary(m => m.Id);
        return Task.FromResult<IReadOnlyList<ShowtimeOccupancy>>(Active()
            .Where(s => s.ScreenId == screenId && s.StartTime >= from && s.StartTime < to)
            .OrderBy(s => s.StartTime)
            .Select(s => new ShowtimeOccupancy(s.Id, s.StartTime, allMovies[s.MovieId].DurationMinutes, allMovies[s.MovieId].Title))
            .ToList());
    }
}
