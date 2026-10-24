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
        _existing = SeedShowtime(Day.AddHours(20));
        _sut = new ShowtimeService(_showtimes, _movies, _unitOfWork);
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
        var request = NewRequest(Day.AddHours(20)) with { ScreenId = Guid.NewGuid() };

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

    // Comportamiento actual (#11 de la auditoría): se informa como conflicto. El lote 8 lo cambia.
    [Fact]
    public async Task Create_WhenMovieDoesNotExist_ThrowsShowtimeConflictException()
    {
        var request = NewRequest(Day.AddHours(10)) with { MovieId = Guid.NewGuid() };

        await Assert.ThrowsAsync<ShowtimeConflictException>(() => _sut.CreateAsync(request, Ct));
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

    private Showtime SeedShowtime(DateTime start)
    {
        var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = _movie.Id, ScreenId = _screenId, StartTime = start, Price = 10m };
        _showtimes.Seed(showtime);
        return showtime;
    }

    private CreateShowtimeRequest NewRequest(DateTime start) => new(_movie.Id, _screenId, start, 10m);

    private static UpdateShowtimeRequest ToUpdate(CreateShowtimeRequest r) => new(r.MovieId, r.ScreenId, r.StartTime, r.Price);
}
