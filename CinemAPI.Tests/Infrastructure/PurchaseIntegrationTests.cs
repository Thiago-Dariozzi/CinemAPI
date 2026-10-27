using Application.DTOs.Purchases;
using Application.Interfaces;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Models;
using Infrastructure.Repositories;

namespace CinemAPI.Tests.Infrastructure;

// Compras contra SQL Server real: el índice único filtrado de butacas y la traducción de las consultas.
[Collection(SqlServerTestGroup.Name)]
public sealed class PurchaseIntegrationTests(SqlServerDatabaseFixture database)
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Tomorrow = DateTime.Now.Date.AddDays(1).AddHours(20);

    // Otra compra de la misma butaca se guarda entre la validación del servicio y su SaveChanges: el
    // índice único la frena y el cliente recibe el 409 de butaca tomada, no un 500.
    [Fact]
    public async Task PurchaseService_Create_WhenAConcurrentPurchaseWinsTheRace_ThrowsSeatConflict()
    {
        var (showtime, buyer, rival) = await SeedShowtimeAndClientsAsync();
        var winner = NewPurchase(showtime, rival, (4, 7));

        await using var context = database.CreateContext();
        var raceBeforeSave = new RaceUnitOfWork(context, () => SaveAsync(winner));
        var sut = new PurchaseService(
            new PurchaseRepository(context), new ShowtimeRepository(context), new MovieRepository(context),
            new ScreenRepository(context), new TicketRepository(context), new FeeRepository(context),
            new FakeCurrentUser().As(buyer), TimeProvider.System, raceBeforeSave);

        var ex = await Assert.ThrowsAsync<SeatConflictException>(
            () => sut.CreateAsync(new CreatePurchaseRequest(showtime.Id, [new SeatRequest(4, 7)]), Ct));

        Assert.IsType<UniqueConstraintConflictException>(ex.InnerException);
        await using var verify = database.CreateContext();
        Assert.Equal([new Seat(4, 7)], await new TicketRepository(verify).GetTakenSeatsAsync(showtime.Id, Ct));
    }

    [Fact]
    public async Task UniqueSeatIndex_LetsACancelledSeatBeSoldAgain()
    {
        var (showtime, buyer, rival) = await SeedShowtimeAndClientsAsync();
        var cancelled = NewPurchase(showtime, buyer, (1, 1));
        cancelled.Status = PurchaseStatus.Cancelled;
        cancelled.Tickets.ForEach(t => t.IsCancelled = true);
        await SaveAsync(cancelled);

        await SaveAsync(NewPurchase(showtime, rival, (1, 1)));

        await using var context = database.CreateContext();
        Assert.Equal([new Seat(1, 1)], await new TicketRepository(context).GetTakenSeatsAsync(showtime.Id, Ct));
    }

    [Fact]
    public async Task PurchaseRepository_Details_KeepDeletedMoviesAndFilterTranslatesToSql()
    {
        var (showtime, buyer, _) = await SeedShowtimeAndClientsAsync();
        var purchase = NewPurchase(showtime, buyer, (2, 2), (2, 1));
        await SaveAsync(purchase);
        await using (var context = database.CreateContext())
        {
            var movie = await context.Movies.FindAsync(showtime.MovieId);
            movie!.IsActive = false;
            await context.SaveChangesAsync();
        }

        await using var read = database.CreateContext();
        var repository = new PurchaseRepository(read);
        var details = await repository.GetDetailsAsync(purchase.Id, Ct);
        var found = await repository.SearchAsync(
            new PurchaseFilter(showtime.MovieId, showtime.Id, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1), PurchaseStatus.Confirmed), Ct);
        var mine = await repository.GetByUserAsync(buyer.Id, Ct);

        Assert.NotNull(details);
        Assert.StartsWith("Película", details.MovieTitle, StringComparison.Ordinal);
        Assert.Equal([new PurchasedSeat(purchase.Tickets[1].Id, 2, 1, 10m), new PurchasedSeat(purchase.Tickets[0].Id, 2, 2, 10m)], details.Seats);
        Assert.Equal([purchase.Id], found.Select(p => p.Id));
        Assert.Equal([purchase.Id], mine.Select(p => p.Id));
    }

    [Fact]
    public async Task TicketRepository_SoldSeatQueries_TranslateAndIgnoreCancelled()
    {
        var (showtime, buyer, rival) = await SeedShowtimeAndClientsAsync();
        var cancelled = NewPurchase(showtime, rival, (5, 9));
        cancelled.Tickets.ForEach(t => t.IsCancelled = true);
        await SaveAsync(NewPurchase(showtime, buyer, (3, 2)), cancelled);

        await using var context = database.CreateContext();
        var repository = new TicketRepository(context);

        Assert.True(await repository.HasSoldTicketsAsync(showtime.Id, Ct));
        Assert.True(await repository.HasSoldSeatsOutsideAsync(showtime.ScreenId, rows: 2, seatsPerRow: 10, DateTime.Now, Ct));
        Assert.False(await repository.HasSoldSeatsOutsideAsync(showtime.ScreenId, rows: 3, seatsPerRow: 2, DateTime.Now, Ct));
        Assert.False(await repository.HasSoldSeatsOutsideAsync(showtime.ScreenId, rows: 2, seatsPerRow: 10, Tomorrow, Ct));
    }

    private async Task<(Showtime Showtime, User Buyer, User Rival)> SeedShowtimeAndClientsAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var genre = new Genre { Id = Guid.NewGuid(), Name = $"Género {suffix}" };
        var movie = new Movie { Id = Guid.NewGuid(), Title = $"Película {suffix}", GenreId = genre.Id, DurationMinutes = 100, IsShowing = true };
        var screen = new Screen { Id = Guid.NewGuid(), Name = $"Sala {suffix}", Rows = 5, SeatsPerRow = 10 };
        var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = movie.Id, ScreenId = screen.Id, StartTime = Tomorrow, Price = 10m };
        var buyer = NewClient("comprador");
        var rival = NewClient("rival");
        await SaveAsync(genre, movie, screen, showtime, buyer, rival);
        return (showtime, buyer, rival);
    }

    private static User NewClient(string name) =>
        new() { Id = Guid.NewGuid(), Name = name, Email = $"{name}.{Guid.NewGuid():N}@cine.com", PasswordHash = "x", Role = Roles.Client };

    private static Purchase NewPurchase(Showtime showtime, User user, params (int Row, int Number)[] seats)
    {
        var purchaseId = Guid.NewGuid();
        return new Purchase
        {
            Id = purchaseId,
            UserId = user.Id,
            ShowtimeId = showtime.Id,
            CreatedAt = DateTime.Now,
            Subtotal = 10m * seats.Length,
            Total = 10m * seats.Length,
            Tickets = seats.Select(s => new Ticket
            {
                Id = Guid.NewGuid(),
                PurchaseId = purchaseId,
                ShowtimeId = showtime.Id,
                SeatRow = s.Row,
                SeatNumber = s.Number,
                Price = 10m,
            }).ToList(),
        };
    }

    private async Task SaveAsync(params object[] entities)
    {
        await using var context = database.CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private sealed class RaceUnitOfWork(IUnitOfWork inner, Func<Task> concurrentRequest) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await concurrentRequest();
            return await inner.SaveChangesAsync(cancellationToken);
        }
    }
}
