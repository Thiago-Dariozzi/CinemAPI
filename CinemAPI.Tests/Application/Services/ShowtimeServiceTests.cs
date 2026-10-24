using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class ShowtimeServiceTests
{
    private static readonly DateTime Day = new(2026, 11, 1);

    private readonly FakeShowtimeRepository _showtimes;
    private readonly FakeMovieRepository _movies = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ShowtimeService _sut;

    private readonly Guid _screenId = Guid.NewGuid();
    private readonly Movie _movie = new() { Id = Guid.NewGuid(), Title = "Película de 120", DurationMinutes = 120 };

    // Función existente 20:00; con 120 min + 15 de limpieza ocupa la sala hasta las 22:15.
    private readonly Showtime _existing;

    public ShowtimeServiceTests()
    {
        _showtimes = new FakeShowtimeRepository(_movies);
        _movies.Seed(_movie);
        _existing = NewShowtime(Day.AddHours(20));
        _existing.Id = Guid.NewGuid();
        _showtimes.Seed(_existing);
        _sut = new ShowtimeService(_showtimes, _movies, _unitOfWork);
    }

    [Theory]
    [InlineData(20, 0, true)]   // mismo horario
    [InlineData(21, 0, true)]   // empieza en el medio de la otra
    [InlineData(18, 0, true)]   // termina 20:15, pisa el comienzo de la otra
    [InlineData(17, 46, true)]  // termina 20:01
    [InlineData(17, 45, false)] // termina justo 20:00: tocarse en el borde no choca
    [InlineData(22, 15, false)] // empieza justo cuando la otra libera la sala
    public async Task Add_DetectsOverlapOnSameScreenAndDay(int hour, int minute, bool shouldConflict)
    {
        var showtime = NewShowtime(Day.AddHours(hour).AddMinutes(minute));

        var act = () => _sut.AddAsync(showtime, CancellationToken.None);

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
    public async Task Add_WhenStartsDuringCleaningBuffer_ThrowsShowtimeConflictException(int minutesAfterTen)
    {
        var showtime = NewShowtime(Day.AddHours(22).AddMinutes(minutesAfterTen));

        await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.AddAsync(showtime, CancellationToken.None));
    }

    [Fact]
    public async Task Add_WhenSameTimeOnAnotherScreen_DoesNotConflict()
    {
        var showtime = NewShowtime(Day.AddHours(20));
        showtime.ScreenId = Guid.NewGuid();

        var created = await _sut.AddAsync(showtime, CancellationToken.None);

        Assert.Contains(created, _showtimes.Stored);
    }

    [Fact]
    public async Task Add_WhenSameTimeOnAnotherDay_DoesNotConflict()
    {
        var showtime = NewShowtime(Day.AddDays(1).AddHours(20));

        var created = await _sut.AddAsync(showtime, CancellationToken.None);

        Assert.Contains(created, _showtimes.Stored);
    }

    [Fact]
    public async Task Add_WhenExistingShowtimeIsInactive_DoesNotConflict()
    {
        _existing.IsActive = false;
        var showtime = NewShowtime(Day.AddHours(20));

        var created = await _sut.AddAsync(showtime, CancellationToken.None);

        Assert.Contains(created, _showtimes.Stored);
    }

    [Fact]
    public async Task Add_GeneratesIdAndActivates_IgnoringClientValues()
    {
        var clientId = Guid.NewGuid();
        var showtime = NewShowtime(Day.AddHours(10));
        showtime.Id = clientId;
        showtime.IsActive = false;

        var created = await _sut.AddAsync(showtime, CancellationToken.None);

        Assert.NotEqual(clientId, created.Id);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActive);
    }

    // Comportamiento actual (#11 de la auditoría): se informa como conflicto. El lote 8 lo cambia.
    [Fact]
    public async Task Add_WhenMovieDoesNotExist_ThrowsShowtimeConflictException()
    {
        var showtime = NewShowtime(Day.AddHours(10));
        showtime.MovieId = Guid.NewGuid();

        await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.AddAsync(showtime, CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenMovingOverItsOwnSlot_DoesNotConflictWithItself()
    {
        var moved = NewShowtime(Day.AddHours(20).AddMinutes(30));
        moved.Id = _existing.Id;

        await _sut.UpdateAsync(moved, CancellationToken.None);

        Assert.Equal(Day.AddHours(20).AddMinutes(30), _showtimes.Stored.Single().StartTime);
    }

    [Fact]
    public async Task Update_WhenOverlapsAnotherShowtime_ThrowsShowtimeConflictException()
    {
        var other = NewShowtime(Day.AddHours(10));
        other.Id = Guid.NewGuid();
        _showtimes.Seed(other);
        var moved = NewShowtime(Day.AddHours(21));
        moved.Id = other.Id;

        await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.UpdateAsync(moved, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_SetsInactiveWithoutRemovingTheRow()
    {
        await _sut.DeleteAsync(_existing.Id, CancellationToken.None);

        Assert.False(Assert.Single(_showtimes.Stored).IsActive);
    }

    private Showtime NewShowtime(DateTime start) => new()
    {
        MovieId = _movie.Id,
        ScreenId = _screenId,
        StartTime = start,
        Price = 10m,
    };
}
