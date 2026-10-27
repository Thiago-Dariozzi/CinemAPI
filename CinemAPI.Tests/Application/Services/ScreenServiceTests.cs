using Application.DTOs.Screens;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class ScreenServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0);
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeMovieRepository _movies = new();
    private readonly FakeScreenRepository _screens = new();
    private readonly FakeShowtimeRepository _showtimes;
    private readonly FakePurchaseRepository _purchases;
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ScreenService _sut;

    // 10 filas (A–J) × 12 butacas.
    private readonly Screen _screen = new() { Id = Guid.NewGuid(), Name = "Sala 1", Rows = 10, SeatsPerRow = 12 };

    public ScreenServiceTests()
    {
        _showtimes = new FakeShowtimeRepository(_movies);
        _purchases = new FakePurchaseRepository(_showtimes, _movies, _screens, new FakeUserRepository());
        _screens.Seed(_screen);
        _sut = new ScreenService(_screens, new FakeTicketRepository(_purchases, _showtimes), new FixedTimeProvider(Now), _unitOfWork);
    }

    [Fact]
    public async Task Create_SavesRowsAndSeatsPerRow_AndCalculatesCapacity()
    {
        var created = await _sut.CreateAsync(new CreateScreenRequest("Sala 2", 8, 15), Ct);

        Assert.Equal((8, 15, 120), (created.Rows, created.SeatsPerRow, created.Capacity));
    }

    [Theory]
    [InlineData(9, 12)] // se va la fila J
    [InlineData(10, 11)] // se va la butaca 12
    public async Task Update_ShrinkingOverASoldSeatOfAFutureShowtime_ThrowsConflict(int rows, int seatsPerRow)
    {
        SeedSoldSeat(Now.AddDays(1), row: 10, number: 12);

        await Assert.ThrowsAsync<ScreenConflictException>(() => _sut.UpdateAsync(_screen.Id, new UpdateScreenRequest("Sala 1", rows, seatsPerRow), Ct));
        Assert.Equal((10, 12), (_screen.Rows, _screen.SeatsPerRow));
    }

    [Fact]
    public async Task Update_ShrinkingWhenOnlyPastShowtimesUsedThoseSeats_IsAllowed()
    {
        SeedSoldSeat(Now.AddDays(-1), row: 10, number: 12);

        await _sut.UpdateAsync(_screen.Id, new UpdateScreenRequest("Sala 1", 5, 5), Ct);

        Assert.Equal((5, 5), (_screen.Rows, _screen.SeatsPerRow));
    }

    [Fact]
    public async Task Delete_WithSoldSeatsForAFutureShowtime_ThrowsConflict()
    {
        SeedSoldSeat(Now.AddDays(1), row: 1, number: 1);

        await Assert.ThrowsAsync<ScreenConflictException>(() => _sut.DeleteAsync(_screen.Id, Ct));
        Assert.True(_screen.IsActive);
    }

    private void SeedSoldSeat(DateTime start, int row, int number)
    {
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Película", DurationMinutes = 90 };
        var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = movie.Id, ScreenId = _screen.Id, StartTime = start, Price = 10m };
        _movies.Seed(movie);
        _showtimes.Seed(showtime);
        var purchaseId = Guid.NewGuid();
        _purchases.Seed(new Purchase
        {
            Id = purchaseId,
            ShowtimeId = showtime.Id,
            Tickets = [new Ticket { Id = Guid.NewGuid(), PurchaseId = purchaseId, ShowtimeId = showtime.Id, SeatRow = row, SeatNumber = number }],
        });
    }
}
