using Domain.Entities;
using Infrastructure.Repositories;

namespace CinemAPI.Tests.Infrastructure;

// Reportes contra SQL Server real: que las agrupaciones se traduzcan a SQL y sumen lo que corresponde.
// Usan fechas de 2033, que ningún otro test de esta base toca.
[Collection(SqlServerTestGroup.Name)]
public sealed class ReportIntegrationTests(SqlServerDatabaseFixture database)
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task GetSalesByMovie_SumsConfirmedPurchasesOfThePeriod_InTheDatabase()
    {
        var periodStart = new DateTime(2033, 3, 1);
        var (genre, screen, user) = await SeedBaseAsync();
        var first = NewMovie(genre, "Primera");
        var second = NewMovie(genre, "Segunda");
        var firstShow = NewShowtime(first, screen, periodStart.AddDays(10));
        var secondShow = NewShowtime(second, screen, periodStart.AddDays(10).AddHours(3));
        await SaveAsync(first, second, firstShow, secondShow);
        await SaveAsync(
            NewPurchase(user, firstShow, periodStart, fee: 2m, seats: 2),                                     // justo en el inicio: cuenta
            NewPurchase(user, firstShow, periodStart.AddDays(5), fee: 1m, seats: 1, firstSeat: 3),
            NewPurchase(user, secondShow, periodStart.AddDays(6), fee: 0m, seats: 3),
            NewPurchase(user, firstShow, periodStart.AddDays(6), fee: 1m, seats: 4, cancelled: true),
            NewPurchase(user, firstShow, periodStart.AddTicks(-1), fee: 1m, seats: 5, firstSeat: 4),          // justo antes del período
            NewPurchase(user, firstShow, periodStart.AddDays(7), fee: 1m, seats: 1, firstSeat: 15),           // justo en el fin: queda afuera
            NewPurchase(user, firstShow, periodStart.AddDays(3), fee: 1m, seats: 6, deleted: true, firstSeat: 9));
        await DeactivateAsync<Movie>(second.Id); // se dio de baja después de vender: sus ventas cuentan igual

        await using var context = database.CreateContext();
        var repository = new ReportRepository(context);
        var stats = await repository.GetSalesByMovieAsync(periodStart, periodStart.AddDays(7), Ct);
        var cancelled = await repository.CountCancelledTicketsAsync(periodStart, periodStart.AddDays(7), Ct);

        var byTitle = stats.ToDictionary(s => s.MovieTitle);
        Assert.Equal(2, stats.Count);
        Assert.Equal((2, 3, 30m, 3m, 33m),
            (byTitle["Primera"].Purchases, byTitle["Primera"].TicketsSold, byTitle["Primera"].Subtotal, byTitle["Primera"].FeeAmount, byTitle["Primera"].Total));
        Assert.Equal((1, 3, 30m, 0m, 30m),
            (byTitle["Segunda"].Purchases, byTitle["Segunda"].TicketsSold, byTitle["Segunda"].Subtotal, byTitle["Segunda"].FeeAmount, byTitle["Segunda"].Total));
        Assert.Equal(4, cancelled);
    }

    [Fact]
    public async Task GetShowtimeSeats_CountsSoldSeatsPerShowtimeOfTheDay()
    {
        var day = new DateTime(2033, 5, 10);
        var (genre, screen, user) = await SeedBaseAsync(); // 4 × 5 = 20 butacas
        var movie = NewMovie(genre, "Ocupación");
        var full = NewShowtime(movie, screen, day.AddHours(15));
        var empty = NewShowtime(movie, screen, day.AddHours(19));
        var deleted = NewShowtime(movie, screen, day.AddHours(22));
        var nextDay = NewShowtime(movie, screen, day.AddDays(1));
        var deletedMovie = NewMovie(genre, "Dada de baja");
        var ofDeletedMovie = NewShowtime(deletedMovie, screen, day.AddHours(23));
        await SaveAsync(movie, deletedMovie, full, empty, deleted, nextDay, ofDeletedMovie);
        await SaveAsync(
            NewPurchase(user, full, day.AddDays(-2), fee: 0m, seats: 3),
            NewPurchase(user, full, day.AddDays(-2), fee: 0m, seats: 2, cancelled: true, firstSeat: 10),
            NewPurchase(user, full, day.AddDays(-2), fee: 0m, seats: 4, deleted: true, firstSeat: 15),
            NewPurchase(user, ofDeletedMovie, day.AddDays(-1), fee: 0m, seats: 1));
        await DeactivateAsync<Showtime>(deleted.Id);
        await DeactivateAsync<Movie>(deletedMovie.Id); // sus funciones del día siguen en el reporte
        await DeactivateAsync<Screen>(screen.Id); // una función pasada de una sala dada de baja sigue en el reporte

        await using var context = database.CreateContext();
        var rows = await new ReportRepository(context).GetShowtimeSeatsAsync(day, day.AddDays(1), Ct);

        Assert.Equal(
            [(full.Id, 20, 3, "Ocupación", screen.Name), (empty.Id, 20, 0, "Ocupación", screen.Name), (ofDeletedMovie.Id, 20, 1, "Dada de baja", screen.Name)],
            rows.Select(r => (r.ShowtimeId, r.Capacity, r.SoldSeats, r.MovieTitle, r.ScreenName)));
    }

    private async Task<(Genre Genre, Screen Screen, User User)> SeedBaseAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var genre = new Genre { Id = Guid.NewGuid(), Name = $"Género {suffix}" };
        var screen = new Screen { Id = Guid.NewGuid(), Name = $"Sala {suffix}", Rows = 4, SeatsPerRow = 5 };
        var user = new User { Id = Guid.NewGuid(), Name = "Comprador", Email = $"c.{suffix}@cine.com", PasswordHash = "x" };
        await SaveAsync(genre, screen, user);
        return (genre, screen, user);
    }

    private static Movie NewMovie(Genre genre, string title) =>
        new() { Id = Guid.NewGuid(), Title = $"{title}", GenreId = genre.Id, DurationMinutes = 100, IsShowing = true };

    private static Showtime NewShowtime(Movie movie, Screen screen, DateTime start) =>
        new() { Id = Guid.NewGuid(), MovieId = movie.Id, ScreenId = screen.Id, StartTime = start, Price = 10m };

    // Entradas de $10 en la fila 1, desde la butaca firstSeat. El total es subtotal + cargo, como en una compra real.
    private static Purchase NewPurchase(
        User user, Showtime showtime, DateTime createdAt, decimal fee, int seats,
        bool cancelled = false, bool deleted = false, int firstSeat = 1)
    {
        var id = Guid.NewGuid();
        var subtotal = 10m * seats;
        return new Purchase
        {
            Id = id,
            UserId = user.Id,
            ShowtimeId = showtime.Id,
            CreatedAt = createdAt,
            Subtotal = subtotal,
            FeeAmount = fee,
            Total = subtotal + fee,
            Status = cancelled ? PurchaseStatus.Cancelled : PurchaseStatus.Confirmed,
            IsActive = !deleted,
            Tickets = Enumerable.Range(firstSeat, seats)
                .Select(n => new Ticket
                {
                    Id = Guid.NewGuid(),
                    PurchaseId = id,
                    ShowtimeId = showtime.Id,
                    SeatRow = deleted ? 2 : 1,
                    SeatNumber = n,
                    Price = 10m,
                    IsCancelled = cancelled,
                    IsActive = !deleted,
                })
                .ToList(),
        };
    }

    private async Task SaveAsync(params object[] entities)
    {
        await using var context = database.CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private async Task DeactivateAsync<T>(Guid id) where T : class, Domain.Interfaces.IEntity
    {
        await using var context = database.CreateContext();
        var entity = await context.Set<T>().FindAsync(id);
        entity!.IsActive = false;
        await context.SaveChangesAsync();
    }
}
