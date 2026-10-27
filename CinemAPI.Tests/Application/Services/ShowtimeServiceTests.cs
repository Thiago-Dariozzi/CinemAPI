using Application.DTOs.Showtimes;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class ShowtimeServiceTests
{
    private static readonly DateTime Day = new(2026, 11, 1);
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeShowtimeRepository _showtimes;
    private readonly FakeMovieRepository _movies = new();
    private readonly FakeScreenRepository _screens = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakePurchaseRepository _purchases;
    private readonly FixedTimeProvider _clock = new(Day.AddDays(-1));
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ShowtimeService _sut;

    private readonly Guid _screenId = Guid.NewGuid();
    private readonly Movie _movie = new() { Id = Guid.NewGuid(), Title = "Película de 120", DurationMinutes = 120 };

    // Función existente 20:00; con 120 min + 15 de limpieza ocupa la sala hasta las 22:15.
    private readonly Showtime _existing;

    public ShowtimeServiceTests()
    {
        _showtimes = new FakeShowtimeRepository(_movies);
        _purchases = new FakePurchaseRepository(_showtimes, _movies, _screens, _users);
        _movies.Seed(_movie);
        _screens.Seed(new Screen { Id = _screenId, Name = "Sala 1", Rows = 10, SeatsPerRow = 10 });
        _existing = SeedShowtime(Day.AddHours(20));
        _sut = new ShowtimeService(_showtimes, _movies, _screens, new FakeTicketRepository(_purchases, _showtimes), _clock, _unitOfWork);
    }

    [Theory]
    [InlineData(20, 0, true)]   // mismo horario
    [InlineData(21, 0, true)]   // empieza en el medio de la otra
    [InlineData(18, 0, true)]   // termina 20:15, pisa el comienzo de la otra
    [InlineData(17, 46, true)]  // termina 20:01
    [InlineData(17, 45, false)] // termina justo 20:00: tocarse en el borde no choca
    [InlineData(22, 15, false)] // empieza justo cuando la otra libera la sala
    public async Task Create_DetectsOverlapOnSameScreenAndDay(int hour, int minute, bool shouldConflict)
    {
        var request = NewRequest(Day.AddHours(hour).AddMinutes(minute));

        var act = () => _sut.CreateAsync(request, Ct);

        if (shouldConflict)
        {
            await Assert.ThrowsAsync<ShowtimeConflictException>(act);
        }
        else
        {
            await act();
        }
    }

    [Theory]
    [InlineData(0)]  // 22:00: la película terminó, pero la sala está en limpieza
    [InlineData(14)] // 22:14: último minuto del buffer
    public async Task Create_WhenStartsDuringCleaningBuffer_ThrowsShowtimeConflictException(int minutesAfterTen)
    {
        var request = NewRequest(Day.AddHours(22).AddMinutes(minutesAfterTen));

        await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.CreateAsync(request, Ct));
    }

    [Fact]
    public async Task Create_WhenSameTimeOnAnotherScreen_DoesNotConflict()
    {
        var otherScreen = new Screen { Id = Guid.NewGuid(), Name = "Sala 2", Rows = 5, SeatsPerRow = 10 };
        _screens.Seed(otherScreen);
        var request = NewRequest(Day.AddHours(20)) with { ScreenId = otherScreen.Id };

        var created = await _sut.CreateAsync(request, Ct);

        Assert.Contains(_showtimes.Stored, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Create_WhenSameTimeOnAnotherDay_DoesNotConflict()
    {
        var created = await _sut.CreateAsync(NewRequest(Day.AddDays(1).AddHours(20)), Ct);

        Assert.Contains(_showtimes.Stored, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Create_WhenExistingShowtimeIsInactive_DoesNotConflict()
    {
        _existing.IsActive = false;

        var created = await _sut.CreateAsync(NewRequest(Day.AddHours(20)), Ct);

        Assert.Contains(_showtimes.Stored, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Create_GeneratesIdActivatesAndSavesOnce()
    {
        var created = await _sut.CreateAsync(NewRequest(Day.AddHours(10)), Ct);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActive);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    // Antes se informaba como conflicto (409); es un error del pedido (#11 de la auditoría).
    [Fact]
    public async Task Create_WhenMovieDoesNotExist_ThrowsInvalidReferenceException()
    {
        var request = NewRequest(Day.AddHours(10)) with { MovieId = Guid.NewGuid() };

        await Assert.ThrowsAsync<InvalidReferenceException>(() => _sut.CreateAsync(request, Ct));
    }

    [Fact]
    public async Task Create_WhenScreenDoesNotExist_ThrowsInvalidReferenceException()
    {
        var request = NewRequest(Day.AddHours(10)) with { ScreenId = Guid.NewGuid() };

        await Assert.ThrowsAsync<InvalidReferenceException>(() => _sut.CreateAsync(request, Ct));
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // Bug #4: antes solo se comparaban funciones del mismo día calendario.
    [Theory]
    [InlineData(0, 30, true)]   // la de las 23:00 del día anterior ocupa la sala hasta la 01:15
    [InlineData(1, 14, true)]
    [InlineData(1, 15, false)]  // empieza justo cuando se libera
    public async Task Create_DetectsOverlapWithShowtimeCrossingMidnight(int hour, int minute, bool shouldConflict)
    {
        SeedShowtime(Day.AddHours(-1)); // 23:00 del día anterior, 120 + 15 min

        var act = () => _sut.CreateAsync(NewRequest(Day.AddHours(hour).AddMinutes(minute)), Ct);

        if (shouldConflict)
        {
            await Assert.ThrowsAsync<ShowtimeConflictException>(act);
        }
        else
        {
            await act();
        }
    }

    [Fact]
    public async Task Create_WhenNewShowtimeCrossesMidnightIntoNextDayShowtime_ThrowsShowtimeConflictException()
    {
        SeedShowtime(Day.AddDays(1).AddMinutes(30)); // 00:30 del día siguiente

        await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.CreateAsync(NewRequest(Day.AddHours(23)), Ct));
    }

    // Bug #5: antes una película dada de baja contaba como 0 minutos y la sala quedaba "libre".
    [Fact]
    public async Task Create_WhenExistingShowtimeMovieWasDeleted_StillBlocksItsWholeDuration()
    {
        var deletedMovie = new Movie { Id = Guid.NewGuid(), Title = "Borrada", DurationMinutes = 120, IsActive = false };
        _movies.Seed(deletedMovie);
        var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = deletedMovie.Id, ScreenId = _screenId, StartTime = Day.AddHours(10) };
        _showtimes.Seed(showtime);

        var ex = await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.CreateAsync(NewRequest(Day.AddHours(11)), Ct));
        Assert.Contains("'Borrada'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Update_WhenMovingOverItsOwnSlot_DoesNotConflictWithItself()
    {
        await _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(20).AddMinutes(30))), Ct);

        Assert.Equal(Day.AddHours(20).AddMinutes(30), _existing.StartTime);
    }

    [Fact]
    public async Task Update_WhenOverlapsAnotherShowtime_ThrowsAndLeavesItUnchanged()
    {
        var other = SeedShowtime(Day.AddHours(10));

        await Assert.ThrowsAsync<ShowtimeConflictException>(
            () => _sut.UpdateAsync(other.Id, ToUpdate(NewRequest(Day.AddHours(21))), Ct));
        Assert.Equal(Day.AddHours(10), other.StartTime);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // Revisión final: antes respondía 400 aunque no se cambiara la película.
    [Fact]
    public async Task Update_WhenItsMovieWasDeleted_CanBeRescheduledWithoutChangingIt()
    {
        _movie.IsActive = false;

        await _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(9))), Ct);

        Assert.Equal(Day.AddHours(9), _existing.StartTime);
    }

    [Fact]
    public async Task Update_ToADeletedMovie_ThrowsInvalidReferenceException()
    {
        var deleted = new Movie { Id = Guid.NewGuid(), Title = "Borrada", DurationMinutes = 90, IsActive = false };
        _movies.Seed(deleted);

        await Assert.ThrowsAsync<InvalidReferenceException>(
            () => _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(9)) with { MovieId = deleted.Id }), Ct));
    }

    [Fact]
    public async Task Update_WhenShowtimeDoesNotExist_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), ToUpdate(NewRequest(Day.AddHours(10))), Ct));
    }

    [Fact]
    public async Task Delete_SetsInactiveWithoutRemovingTheRow()
    {
        await _sut.DeleteAsync(_existing.Id, Ct);

        Assert.False(Assert.Single(_showtimes.Stored).IsActive);
    }

    [Fact]
    public async Task Delete_WhenAlreadyDeleted_ThrowsNotFoundException()
    {
        _existing.IsActive = false;

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(_existing.Id, Ct));
    }

    [Fact]
    public async Task Delete_WithSoldTickets_ThrowsConflict_ButCancelledOnesDoNotCount()
    {
        var purchase = SeedPurchase(_existing, (1, 1));

        await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.DeleteAsync(_existing.Id, Ct));

        purchase.Tickets.ForEach(t => t.IsCancelled = true);
        await _sut.DeleteAsync(_existing.Id, Ct);
        Assert.False(_existing.IsActive);
    }

    [Fact]
    public async Task Update_ToAnotherScreen_WithSoldTickets_ThrowsConflict()
    {
        var otherScreen = new Screen { Id = Guid.NewGuid(), Name = "Sala 2", Rows = 5, SeatsPerRow = 10 };
        _screens.Seed(otherScreen);
        SeedPurchase(_existing, (1, 1));

        await Assert.ThrowsAsync<ShowtimeConflictException>(
            () => _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(20)) with { ScreenId = otherScreen.Id }), Ct));
        Assert.Equal(_screenId, _existing.ScreenId);
    }

    [Fact]
    public async Task Update_WithSoldTickets_CannotChangeTheMovie()
    {
        var otherMovie = new Movie { Id = Guid.NewGuid(), Title = "Otra", DurationMinutes = 90 };
        _movies.Seed(otherMovie);
        SeedPurchase(_existing, (1, 1));

        await Assert.ThrowsAsync<ShowtimeConflictException>(
            () => _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(20)) with { MovieId = otherMovie.Id }), Ct));
        Assert.Equal(_movie.Id, _existing.MovieId);
    }

    [Fact]
    public async Task Update_WithSoldTickets_CannotBeRescheduled_ButCancelledOnesDoNotCount()
    {
        var purchase = SeedPurchase(_existing, (1, 1));

        var ex = await Assert.ThrowsAsync<ShowtimeConflictException>(
            () => _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(16))), Ct));
        Assert.Contains("horario", ex.Message, StringComparison.Ordinal);
        Assert.Equal(Day.AddHours(20), _existing.StartTime);

        purchase.Tickets.ForEach(t => t.IsCancelled = true);
        await _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(16))), Ct);
        Assert.Equal(Day.AddHours(16), _existing.StartTime);
    }

    [Fact]
    public async Task Update_WithSoldTickets_CanChangeThePrice()
    {
        SeedPurchase(_existing, (1, 1));

        await _sut.UpdateAsync(_existing.Id, ToUpdate(NewRequest(Day.AddHours(20)) with { Price = 15m }), Ct);

        Assert.Equal(15m, _existing.Price);
    }

    [Fact]
    public async Task GetSeatMap_ReturnsTheLayoutAndOnlyTheSoldSeats()
    {
        SeedPurchase(_existing, (2, 3), (2, 4));
        SeedPurchase(_existing, (5, 5)).Tickets.ForEach(t => t.IsCancelled = true);

        var map = await _sut.GetSeatMapAsync(_existing.Id, Ct);

        Assert.Equal((10, 10, 10m, true), (map.Rows, map.SeatsPerRow, map.Price, map.IsOnSale));
        Assert.Equal([(2, 3), (2, 4)], map.TakenSeats.Select(s => (s.Row, s.Number)));
    }

    [Fact]
    public async Task GetSeatMap_OfAStartedShowtime_IsNotOnSale()
    {
        _clock.Now = _existing.StartTime;

        Assert.False((await _sut.GetSeatMapAsync(_existing.Id, Ct)).IsOnSale);
    }

    [Fact]
    public async Task GetUpcoming_ReturnsOnlyShowtimesThatHaveNotStarted()
    {
        var past = SeedShowtime(Day.AddDays(-2));

        var result = await _sut.GetUpcomingAsync(_movie.Id, Ct);

        Assert.Equal([_existing.Id], result.Select(s => s.Id));
        Assert.DoesNotContain(result, s => s.Id == past.Id);
    }

    private Purchase SeedPurchase(Showtime showtime, params (int Row, int Number)[] seats)
    {
        var purchaseId = Guid.NewGuid();
        var purchase = new Purchase
        {
            Id = purchaseId,
            UserId = Guid.NewGuid(),
            ShowtimeId = showtime.Id,
            Tickets = seats.Select(s => new Ticket { Id = Guid.NewGuid(), PurchaseId = purchaseId, ShowtimeId = showtime.Id, SeatRow = s.Row, SeatNumber = s.Number }).ToList(),
        };
        _purchases.Seed(purchase);
        return purchase;
    }

    private Showtime SeedShowtime(DateTime start)
    {
        var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = _movie.Id, ScreenId = _screenId, StartTime = start, Price = 10m };
        _showtimes.Seed(showtime);
        return showtime;
    }

    private CreateShowtimeRequest NewRequest(DateTime start) => new(_movie.Id, _screenId, start, 10m);

    private static UpdateShowtimeRequest ToUpdate(CreateShowtimeRequest r) => new(r.MovieId, r.ScreenId, r.StartTime, r.Price);
}
