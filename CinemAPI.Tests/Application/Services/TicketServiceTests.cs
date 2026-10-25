using Application.DTOs.Tickets;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class TicketServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeTicketRepository _tickets = new();
    private readonly FakeMovieRepository _movies = new();
    private readonly FakeScreenRepository _screens = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly TicketService _sut;

    private readonly Movie _movie = new() { Id = Guid.NewGuid(), Title = "Película", DurationMinutes = 100 };
    private readonly Screen _screen = new() { Id = Guid.NewGuid(), Name = "Sala 1", Capacity = 100 };
    private readonly User _user = new() { Id = Guid.NewGuid(), Name = "Ana", Email = "ana@cine.com", Password = "x" };

    public TicketServiceTests()
    {
        _movies.Seed(_movie);
        _screens.Seed(_screen);
        _users.Seed(_user);
        _sut = new TicketService(_tickets, _movies, _screens, _users, _unitOfWork);
    }

    [Fact]
    public async Task Create_WithValidReferences_Saves()
    {
        var created = await _sut.CreateAsync(NewRequest(), Ct);

        Assert.Contains(_tickets.Stored, t => t.Id == created.Id);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    // Bug #3: antes cualquier Id inexistente terminaba en una violación de FK (500).
    [Theory]
    [InlineData("movie")]
    [InlineData("screen")]
    [InlineData("user")]
    public async Task Create_WhenAReferenceDoesNotExist_ThrowsInvalidReferenceException(string missing)
    {
        var request = missing switch
        {
            "movie" => NewRequest() with { MovieId = Guid.NewGuid() },
            "screen" => NewRequest() with { ScreenId = Guid.NewGuid() },
            _ => NewRequest() with { UserId = Guid.NewGuid() },
        };

        await Assert.ThrowsAsync<InvalidReferenceException>(() => _sut.CreateAsync(request, Ct));
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Update_WhenItsMovieWasDeleted_CanStillChangeThePrice()
    {
        var created = await _sut.CreateAsync(NewRequest(), Ct);
        _movie.IsActive = false;
        var r = NewRequest();

        await _sut.UpdateAsync(created.Id, new UpdateTicketRequest(r.MovieId, r.ScreenId, r.UserId, r.BuyDate, 99m), Ct);

        Assert.Equal(99m, Assert.Single(_tickets.Stored).FinalPrice);
    }

    private CreateTicketRequest NewRequest() =>
        new(_movie.Id, _screen.Id, _user.Id, new DateTime(2026, 11, 1), 10m);
}
