using Application.DTOs.Purchases;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class PurchaseServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0);
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeMovieRepository _movies = new();
    private readonly FakeScreenRepository _screens = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeFeeRepository _fees = new();
    private readonly FakeShowtimeRepository _showtimes;
    private readonly FakePurchaseRepository _purchases;
    private readonly FakeTicketRepository _tickets;
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FixedTimeProvider _clock = new(Now);
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly PurchaseService _sut;

    private readonly Movie _movie = new() { Id = Guid.NewGuid(), Title = "Película", DurationMinutes = 100, IsShowing = true };

    // 5 filas (A–E) × 8 butacas.
    private readonly Screen _screen = new() { Id = Guid.NewGuid(), Name = "Sala 1", Rows = 5, SeatsPerRow = 8 };
    private readonly User _client = new() { Id = Guid.NewGuid(), Name = "Ana", Email = "ana@cine.com", Role = Roles.Client };
    private readonly User _otherClient = new() { Id = Guid.NewGuid(), Name = "Beto", Email = "beto@cine.com", Role = Roles.Client };
    private readonly User _admin = new() { Id = Guid.NewGuid(), Name = "Admin", Email = "admin@cine.com", Role = Roles.Admin };
    private readonly Showtime _showtime;

    public PurchaseServiceTests()
    {
        _showtimes = new FakeShowtimeRepository(_movies);
        _purchases = new FakePurchaseRepository(_showtimes, _movies, _screens, _users);
        _tickets = new FakeTicketRepository(_purchases, _showtimes);
        _movies.Seed(_movie);
        _screens.Seed(_screen);
        _users.Seed(_client, _otherClient, _admin);
        _showtime = SeedShowtime(Now.AddDays(1), price: 10m);
        _currentUser.As(_client);
        _sut = new PurchaseService(_purchases, _showtimes, _movies, _screens, _tickets, _fees, _currentUser, _clock, _unitOfWork);
    }

    [Fact]
    public async Task Create_WithFreeSeatsAndNoFee_SavesThePurchaseWithItsTickets()
    {
        var result = await _sut.CreateAsync(Request(_showtime, (1, 1), (1, 2), (2, 5)), Ct);

        Assert.Equal(1, _unitOfWork.SaveCount);
        var saved = Assert.Single(_purchases.Stored);
        Assert.Equal(_client.Id, saved.UserId);
        Assert.Equal((30m, 0m, 30m), (result.Subtotal, result.FeeAmount, result.Total));
        Assert.Null(saved.FeeId);
        Assert.Equal(PurchaseStatus.Confirmed, result.Status);
        Assert.Equal(Now, result.CreatedAt);
        Assert.Equal([(1, 1), (1, 2), (2, 5)], result.Tickets.Select(t => (t.Row, t.Number)));
        Assert.All(result.Tickets, t => Assert.Equal(10m, t.Price));
        Assert.True(result.CanCancel);
        Assert.Equal(("Película", "Sala 1", "Ana"), (result.MovieTitle, result.ScreenName, result.UserName));
    }

    [Fact]
    public async Task Create_WithACurrentFee_AddsPercentageAndFixedAmountAndKeepsTheFee()
    {
        var showtime = SeedShowtime(Now.AddDays(2), price: 10.50m);
        var fee = new Fee { Id = Guid.NewGuid(), Name = "Servicio", Percentage = 10m, FixedAmount = 1.50m, IsCurrent = true };
        _fees.Seed(new Fee { Id = Guid.NewGuid(), Name = "Viejo", Percentage = 50m, IsCurrent = false }, fee);

        var result = await _sut.CreateAsync(Request(showtime, (1, 1), (1, 2), (1, 3)), Ct);

        // 3 × 10,50 = 31,50; 10 % = 3,15 + 1,50 fijo = 4,65.
        Assert.Equal((31.50m, 4.65m, 36.15m), (result.Subtotal, result.FeeAmount, result.Total));
        Assert.Equal(fee.Id, Assert.Single(_purchases.Stored).FeeId);
    }

    [Theory]
    [InlineData(3.33, 15, 0, 0.50)]  // 0,4995 → 0,50 (redondeo comercial)
    [InlineData(10.05, 10, 0, 1.01)] // 1,005 justo en el medio → sube (con redondeo bancario daría 1,00)
    [InlineData(10, 0, 2.5, 2.50)]   // solo monto fijo
    [InlineData(10, 12.5, 0, 1.25)]  // solo porcentaje
    public async Task Create_RoundsTheFeeToCents(decimal price, decimal percentage, decimal fixedAmount, decimal expectedFee)
    {
        var showtime = SeedShowtime(Now.AddDays(3), price);
        _fees.Seed(new Fee { Id = Guid.NewGuid(), Name = "Cargo", Percentage = percentage, FixedAmount = fixedAmount, IsCurrent = true });

        var result = await _sut.CreateAsync(Request(showtime, (1, 1)), Ct);

        Assert.Equal(expectedFee, result.FeeAmount);
        Assert.Equal(price + expectedFee, result.Total);
    }

    [Theory]
    [InlineData(0)]    // empieza justo ahora
    [InlineData(-30)]  // empezó hace media hora
    public async Task Create_WhenTheShowtimeAlreadyStarted_Throws(int minutesFromNow)
    {
        var showtime = SeedShowtime(Now.AddMinutes(minutesFromNow), price: 10m);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(Request(showtime, (1, 1)), Ct));

        Assert.Contains("ya empezó", ex.Message, StringComparison.Ordinal);
        Assert.Empty(_purchases.Stored);
    }

    [Fact]
    public async Task Create_WhenTheMovieIsNotShowing_Throws()
    {
        _movie.IsShowing = false;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(Request(_showtime, (1, 1)), Ct));

        Assert.Contains("no está en cartelera", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_WhenTheMovieWasDeleted_Throws()
    {
        _movie.IsActive = false;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(Request(_showtime, (1, 1)), Ct));
    }

    [Fact]
    public async Task Create_WhenTheShowtimeDoesNotExist_ThrowsInvalidReference()
    {
        var request = new CreatePurchaseRequest(Guid.NewGuid(), [new SeatRequest(1, 1)]);

        await Assert.ThrowsAsync<InvalidReferenceException>(() => _sut.CreateAsync(request, Ct));
    }

    [Fact]
    public async Task Create_WhenASeatIsTaken_ThrowsConflictNamingTheSeats()
    {
        await _sut.CreateAsync(Request(_showtime, (2, 3), (2, 4)), Ct);
        _currentUser.As(_otherClient);

        var ex = await Assert.ThrowsAsync<SeatConflictException>(() => _sut.CreateAsync(Request(_showtime, (2, 2), (2, 3), (2, 4)), Ct));

        Assert.Contains("B3, B4", ex.Message, StringComparison.Ordinal);
        Assert.Single(_purchases.Stored);
    }

    [Fact]
    public async Task Create_TheSameSeatInAnotherShowtime_IsFree()
    {
        var other = SeedShowtime(Now.AddDays(2), price: 10m);
        await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);

        await _sut.CreateAsync(Request(other, (1, 1)), Ct);

        Assert.Equal(2, _purchases.Stored.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task Create_WithoutSeatsOrMoreThanTen_Throws(int count)
    {
        var seats = Enumerable.Range(1, count).Select(n => (1 + (n - 1) / 8, 1 + (n - 1) % 8)).ToArray();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(Request(_showtime, seats), Ct));

        Assert.Contains("entre 1 y 10", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_WithTenSeats_IsAllowed()
    {
        var seats = Enumerable.Range(1, 10).Select(n => (1 + (n - 1) / 8, 1 + (n - 1) % 8)).ToArray();

        var result = await _sut.CreateAsync(Request(_showtime, seats), Ct);

        Assert.Equal(10, result.Tickets.Count);
    }

    // [Required] no valida los elementos de la lista: antes daba un 500.
    [Fact]
    public async Task Create_WithANullSeat_Throws()
    {
        var request = new CreatePurchaseRequest(_showtime.Id, [new SeatRequest(1, 1), null!]);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request, Ct));
        Assert.Empty(_purchases.Stored);
    }

    [Fact]
    public async Task Create_WithARepeatedSeat_Throws()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(Request(_showtime, (1, 1), (1, 1)), Ct));

        Assert.Contains("A1", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(6, 1)] // la sala tiene filas A–E
    [InlineData(1, 9)] // y 8 butacas por fila
    public async Task Create_WithASeatOutsideTheScreen_Throws(int row, int number)
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(Request(_showtime, (1, 1), (row, number)), Ct));

        Assert.Contains("no tiene la butaca", ex.Message, StringComparison.Ordinal);
        Assert.Empty(_purchases.Stored);
    }

    // Dos compras simultáneas pasan juntas la validación: el índice único de la base rechaza la segunda.
    [Fact]
    public async Task Create_WhenTheDatabaseRejectsTheSeat_ThrowsSeatConflict()
    {
        _unitOfWork.OnSave = () => throw new UniqueConstraintConflictException(new InvalidOperationException());

        var ex = await Assert.ThrowsAsync<SeatConflictException>(() => _sut.CreateAsync(Request(_showtime, (1, 1)), Ct));

        Assert.Contains("se acaba de vender", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(121, true)]  // faltan más de 2 h
    [InlineData(120, false)] // justo 2 h: ya no
    [InlineData(30, false)]
    public async Task Cancel_ByTheOwner_OnlyMoreThanTwoHoursBefore(int minutesBeforeStart, bool allowed)
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (3, 3)), Ct);
        _clock.Now = _showtime.StartTime.AddMinutes(-minutesBeforeStart);

        if (allowed)
        {
            var result = await _sut.CancelMineAsync(purchase.Id, Ct);
            Assert.Equal(PurchaseStatus.Cancelled, result.Status);
            Assert.Equal(_clock.Now, result.CancelledAt);
            Assert.False(result.CanCancel);
        }
        else
        {
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CancelMineAsync(purchase.Id, Ct));
            Assert.Contains("2 horas", ex.Message, StringComparison.Ordinal);
            Assert.Equal(PurchaseStatus.Confirmed, _purchases.Stored.Single().Status);
        }
    }

    [Fact]
    public async Task Cancel_FreesTheSeats()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (3, 3), (3, 4)), Ct);

        await _sut.CancelMineAsync(purchase.Id, Ct);

        Assert.All(_purchases.Stored.Single().Tickets, t => Assert.True(t.IsCancelled));
        Assert.Empty(await _tickets.GetTakenSeatsAsync(_showtime.Id, Ct));
        _currentUser.As(_otherClient);
        await _sut.CreateAsync(Request(_showtime, (3, 3)), Ct);
    }

    [Fact]
    public async Task Cancel_APurchaseOfAnotherClient_ThrowsForbidden()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);
        _currentUser.As(_otherClient);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.CancelMineAsync(purchase.Id, Ct));
        Assert.Equal(PurchaseStatus.Confirmed, _purchases.Stored.Single().Status);
    }

    [Fact]
    public async Task Cancel_ByStaff_WorksEvenAfterTheShowtimeStarted()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);
        _clock.Now = _showtime.StartTime.AddMinutes(10);
        _currentUser.As(_admin);

        var result = await _sut.CancelAnyAsync(purchase.Id, Ct);

        Assert.Equal(PurchaseStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CancelMine_OnAnotherClientsCancelledPurchase_ThrowsForbidden_NotConflict()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);
        await _sut.CancelMineAsync(purchase.Id, Ct);
        _currentUser.As(_otherClient);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.CancelMineAsync(purchase.Id, Ct));
    }

    [Fact]
    public async Task CancelAny_ByAClient_ThrowsForbidden_EvenForTheirOwnPurchase()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.CancelAnyAsync(purchase.Id, Ct));
        Assert.Equal(PurchaseStatus.Confirmed, _purchases.Stored.Single().Status);
    }

    [Fact]
    public async Task CancelMine_ByStaff_ThrowsForbidden_ForAPurchaseOfAClient()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);
        _currentUser.As(_admin);

        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.CancelMineAsync(purchase.Id, Ct));
    }

    [Fact]
    public async Task Cancel_Twice_ThrowsConflict()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);
        await _sut.CancelMineAsync(purchase.Id, Ct);

        await Assert.ThrowsAsync<PurchaseConflictException>(() => _sut.CancelMineAsync(purchase.Id, Ct));
    }

    [Fact]
    public async Task Cancel_WhenThePurchaseDoesNotExist_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CancelMineAsync(Guid.NewGuid(), Ct));
    }

    [Fact]
    public async Task GetById_OfAnotherClient_ThrowsForbidden_ButStaffCanSeeIt()
    {
        var purchase = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);

        _currentUser.As(_otherClient);
        await Assert.ThrowsAsync<ForbiddenException>(() => _sut.GetByIdAsync(purchase.Id, Ct));

        _currentUser.As(_admin);
        Assert.Equal(purchase.Id, (await _sut.GetByIdAsync(purchase.Id, Ct)).Id);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyThePurchasesOfTheTokenUser()
    {
        var mine = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);
        _currentUser.As(_otherClient);
        await _sut.CreateAsync(Request(_showtime, (1, 2)), Ct);
        _currentUser.As(_client);

        var result = await _sut.GetMineAsync(Ct);

        Assert.Equal([mine.Id], result.Select(p => p.Id));
    }

    [Fact]
    public async Task Search_FiltersByMovieShowtimeDayAndStatus()
    {
        var otherMovie = new Movie { Id = Guid.NewGuid(), Title = "Otra", DurationMinutes = 90, IsShowing = true };
        _movies.Seed(otherMovie);
        var otherShowtime = SeedShowtime(Now.AddDays(1).AddHours(4), price: 8m, otherMovie);
        var first = await _sut.CreateAsync(Request(_showtime, (1, 1)), Ct);
        _clock.Now = Now.AddDays(-3);
        var older = await _sut.CreateAsync(Request(_showtime, (1, 2)), Ct);
        var cancelled = await _sut.CreateAsync(Request(otherShowtime, (1, 1)), Ct);
        await _sut.CancelMineAsync(cancelled.Id, Ct);
        _currentUser.As(_admin);

        Assert.Equal([first.Id, older.Id], Ids(await _sut.SearchAsync(new(_movie.Id, null, null, null, null), Ct)));
        Assert.Equal([cancelled.Id], Ids(await _sut.SearchAsync(new(null, otherShowtime.Id, null, null, null), Ct)));
        Assert.Equal([first.Id], Ids(await _sut.SearchAsync(new(null, null, DateOnly.FromDateTime(Now), DateOnly.FromDateTime(Now), null), Ct)));
        Assert.Equal([cancelled.Id], Ids(await _sut.SearchAsync(new(null, null, null, null, PurchaseStatus.Cancelled), Ct)));
    }

    [Theory]
    [InlineData("Pagada")]
    [InlineData("confirmed")]
    public async Task Search_WithAnUnknownStatus_Throws(string status)
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SearchAsync(new(null, null, null, null, status), Ct));
    }

    [Fact]
    public async Task Search_WithFromAfterTo_Throws()
    {
        var day = DateOnly.FromDateTime(Now);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SearchAsync(new(null, null, day, day.AddDays(-1), null), Ct));
    }

    private static List<Guid> Ids(IEnumerable<PurchaseResponse> purchases) => purchases.Select(p => p.Id).ToList();

    private static CreatePurchaseRequest Request(Showtime showtime, params (int Row, int Number)[] seats) =>
        new(showtime.Id, seats.Select(s => new SeatRequest(s.Row, s.Number)).ToList());

    private Showtime SeedShowtime(DateTime start, decimal price, Movie? movie = null)
    {
        var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = (movie ?? _movie).Id, ScreenId = _screen.Id, StartTime = start, Price = price };
        _showtimes.Seed(showtime);
        return showtime;
    }
}
