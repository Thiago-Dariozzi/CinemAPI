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

    public Task<IReadOnlyList<Showtime>> GetUpcomingAsync(Guid? movieId, DateTime after, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Showtime>>(Active()
            .Where(s => s.StartTime > after && (movieId is null || s.MovieId == movieId))
            .OrderBy(s => s.StartTime)
            .ToList());

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

// Las entradas viven dentro de las compras, como en la base: se leen de FakePurchaseRepository.
public sealed class FakeTicketRepository(FakePurchaseRepository purchases, FakeShowtimeRepository showtimes) : ITicketRepository
{
    public Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Ticket>>(Active().ToList());

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Active().FirstOrDefault(t => t.Id == id));

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(Active().Any(t => t.Id == id));

    public void Add(Ticket entity) => throw new NotSupportedException("Las entradas se agregan dentro de una compra.");

    public Task<IReadOnlyList<Seat>> GetTakenSeatsAsync(Guid showtimeId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Seat>>(Sold()
            .Where(t => t.ShowtimeId == showtimeId)
            .OrderBy(t => t.SeatRow).ThenBy(t => t.SeatNumber)
            .Select(t => new Seat(t.SeatRow, t.SeatNumber))
            .ToList());

    public Task<bool> HasSoldTicketsAsync(Guid showtimeId, CancellationToken ct) =>
        Task.FromResult(Sold().Any(t => t.ShowtimeId == showtimeId));

    public Task<bool> HasSoldSeatsOutsideAsync(Guid screenId, int rows, int seatsPerRow, DateTime after, CancellationToken ct)
    {
        var showtimeById = showtimes.Stored.Where(s => s.IsActive).ToDictionary(s => s.Id);
        return Task.FromResult(Sold().Any(t =>
            showtimeById.TryGetValue(t.ShowtimeId, out var s) && s.ScreenId == screenId && s.StartTime > after
            && (t.SeatRow > rows || t.SeatNumber > seatsPerRow)));
    }

    // Como el filtro global de Tickets: solo mira la entrada (no la compra), igual que TicketRepository.
    private IEnumerable<Ticket> Active() =>
        purchases.Stored.SelectMany(p => p.Tickets).Where(t => t.IsActive);

    private IEnumerable<Ticket> Sold() => Active().Where(t => !t.IsCancelled);
}

public sealed class FakeFeeRepository : InMemoryRepository<Fee>, IFeeRepository
{
    public Task<Fee?> GetCurrentAsync(CancellationToken ct) => Task.FromResult(Active().FirstOrDefault(f => f.IsCurrent));
}

// Arma los detalles con los datos de los otros fakes, incluidos los dados de baja (como el join real).
public sealed class FakePurchaseRepository(
    FakeShowtimeRepository showtimes,
    FakeMovieRepository movies,
    FakeScreenRepository screens,
    FakeUserRepository users) : InMemoryRepository<Purchase>, IPurchaseRepository
{
    public Task<PurchaseDetails?> GetDetailsAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Details().FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<PurchaseDetails>> GetByUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PurchaseDetails>>(Details().Where(p => p.UserId == userId).OrderByDescending(p => p.StartTime).ToList());

    public Task<IReadOnlyList<PurchaseDetails>> SearchAsync(PurchaseFilter filter, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PurchaseDetails>>(Details()
            .Where(p => filter.MovieId is null || p.MovieId == filter.MovieId)
            .Where(p => filter.ShowtimeId is null || p.ShowtimeId == filter.ShowtimeId)
            .Where(p => filter.From is null || p.CreatedAt >= filter.From)
            .Where(p => filter.To is null || p.CreatedAt < filter.To)
            .Where(p => string.IsNullOrEmpty(filter.Status) || p.Status == filter.Status)
            .OrderByDescending(p => p.CreatedAt)
            .ToList());

    private IEnumerable<PurchaseDetails> Details() => Active().Select(p =>
    {
        var showtime = showtimes.Stored.Single(s => s.Id == p.ShowtimeId);
        var movie = movies.Stored.Single(m => m.Id == showtime.MovieId);
        var screen = screens.Stored.Single(s => s.Id == showtime.ScreenId);
        var user = users.Stored.FirstOrDefault(u => u.Id == p.UserId);
        return new PurchaseDetails(
            p.Id, p.UserId, user?.Name ?? string.Empty, user?.Email ?? string.Empty,
            showtime.Id, showtime.StartTime, movie.Id, movie.Title, screen.Id, screen.Name,
            p.CreatedAt, p.Subtotal, p.FeeAmount, p.Total, p.Status, p.CancelledAt,
            p.Tickets.Where(t => t.IsActive).OrderBy(t => t.SeatRow).ThenBy(t => t.SeatNumber)
                .Select(t => new PurchasedSeat(t.Id, t.SeatRow, t.SeatNumber, t.Price)).ToList());
    });
}
