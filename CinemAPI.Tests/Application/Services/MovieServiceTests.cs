using Application.DTOs.Movies;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class MovieServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly FakeMovieRepository _movies = new();
    private readonly FakeGenreRepository _genres = new();
    private readonly FakeShowtimeRepository _showtimes;
    private readonly FixedTimeProvider _clock = new(new DateTime(2026, 11, 2, 15, 0, 0));
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MovieService _sut;

    private readonly Genre _drama = new() { Id = Guid.NewGuid(), Name = "Drama" };

    public MovieServiceTests()
    {
        _genres.Seed(_drama);
        _showtimes = new FakeShowtimeRepository(_movies);
        _sut = new MovieService(_movies, _genres, _showtimes, _clock, _unitOfWork);
    }

    // Bug #3: antes un GenreId inexistente terminaba en una violación de FK (500).
    [Fact]
    public async Task Create_WhenGenreDoesNotExist_ThrowsInvalidReferenceException()
    {
        await Assert.ThrowsAsync<InvalidReferenceException>(() => _sut.CreateAsync(NewRequest(Guid.NewGuid()), Ct));
        Assert.Empty(_movies.Stored);
    }

    [Fact]
    public async Task Create_WhenGenreIsInactive_ThrowsInvalidReferenceException()
    {
        _drama.IsActive = false;

        await Assert.ThrowsAsync<InvalidReferenceException>(() => _sut.CreateAsync(NewRequest(_drama.Id), Ct));
    }

    [Fact]
    public async Task Create_DefaultsToShowing_AndKeepsIsActiveForSoftDeleteOnly()
    {
        var created = await _sut.CreateAsync(NewRequest(_drama.Id), Ct);

        Assert.True(created.IsShowing);
        Assert.True(created.IsActive);
    }

    // #13: "fuera de cartelera" es IsShowing = false; la película sigue existiendo (no es una baja).
    [Fact]
    public async Task Update_SettingNotShowing_KeepsTheMovieActiveAndListed()
    {
        var created = await _sut.CreateAsync(NewRequest(_drama.Id), Ct);

        await _sut.UpdateAsync(created.Id, ToUpdate(NewRequest(_drama.Id)) with { IsShowing = false }, Ct);

        var listed = Assert.Single(await _sut.GetAllAsync(Ct));
        Assert.False(listed.IsShowing);
        Assert.True(listed.IsActive);
    }

    [Fact]
    public async Task GetShowingMovies_ExcludesMoviesNotShowing()
    {
        var showing = await _sut.CreateAsync(NewRequest(_drama.Id), Ct);
        await _sut.CreateAsync(NewRequest(_drama.Id) with { IsShowing = false }, Ct);

        var listed = await _sut.GetShowingMoviesAsync(genreId: null, date: null, Ct);

        Assert.Equal(showing.Id, Assert.Single(listed).Id);
    }

    [Fact]
    public async Task GetShowingMovies_ByGenre_OnlyReturnsThatGenre()
    {
        var comedy = new Genre { Id = Guid.NewGuid(), Name = "Comedia" };
        _genres.Seed(comedy);
        var drama = await _sut.CreateAsync(NewRequest(_drama.Id), Ct);
        await _sut.CreateAsync(NewRequest(comedy.Id), Ct);

        var listed = await _sut.GetShowingMoviesAsync(_drama.Id, date: null, Ct);

        Assert.Equal(drama.Id, Assert.Single(listed).Id);
    }

    // El reloj marca el 2/11 a las 15:00.
    [Fact]
    public async Task GetShowingMovies_ByDate_OnlyMoviesWithAShowtimeThatDayThatHasNotStarted()
    {
        var tonight = await CreateWithShowtimeAsync(new DateTime(2026, 11, 2, 21, 0, 0));
        await CreateWithShowtimeAsync(new DateTime(2026, 11, 2, 11, 0, 0)); // ya empezó
        await CreateWithShowtimeAsync(new DateTime(2026, 11, 3, 0, 0, 0));  // es del día siguiente
        var deletedShowtime = await CreateWithShowtimeAsync(new DateTime(2026, 11, 2, 22, 0, 0));
        _showtimes.Stored.Single(s => s.MovieId == deletedShowtime).IsActive = false;
        await _sut.CreateAsync(NewRequest(_drama.Id), Ct); // sin funciones

        var listed = await _sut.GetShowingMoviesAsync(genreId: null, new DateOnly(2026, 11, 2), Ct);

        Assert.Equal(tonight, Assert.Single(listed).Id);
    }

    // Igual que la compra y el schedule: una función que empieza justo ahora ya no está a la venta.
    [Fact]
    public async Task GetShowingMovies_ByDate_AShowtimeStartingRightNowDoesNotCount()
    {
        await CreateWithShowtimeAsync(new DateTime(2026, 11, 2, 15, 0, 0));

        Assert.Empty(await _sut.GetShowingMoviesAsync(genreId: null, new DateOnly(2026, 11, 2), Ct));
    }

    [Fact]
    public async Task GetShowingMovies_ByAFutureDate_IncludesMidnight_AndExcludesTheNextMidnight()
    {
        var atMidnight = await CreateWithShowtimeAsync(new DateTime(2026, 11, 5, 0, 0, 0));
        var lastMinute = await CreateWithShowtimeAsync(new DateTime(2026, 11, 5, 23, 59, 0));
        await CreateWithShowtimeAsync(new DateTime(2026, 11, 6, 0, 0, 0));

        var listed = await _sut.GetShowingMoviesAsync(genreId: null, new DateOnly(2026, 11, 5), Ct);

        Assert.Equal(new[] { atMidnight, lastMinute }.Order(), listed.Select(m => m.Id).Order());
    }

    [Fact]
    public async Task GetShowingMovies_ByDate_CombinesWithTheGenre_AndAPastDayHasNone()
    {
        var movie = await CreateWithShowtimeAsync(new DateTime(2026, 11, 4, 20, 0, 0));

        Assert.Single(await _sut.GetShowingMoviesAsync(_drama.Id, new DateOnly(2026, 11, 4), Ct));
        Assert.Empty(await _sut.GetShowingMoviesAsync(Guid.NewGuid(), new DateOnly(2026, 11, 4), Ct));
        Assert.Empty(await _sut.GetShowingMoviesAsync(genreId: null, new DateOnly(2026, 11, 1), Ct));
        Assert.Equal(movie, Assert.Single(await _sut.GetShowingMoviesAsync(genreId: null, date: null, Ct)).Id);
    }

    // Antes respondía 500: el filtro pide el día siguiente y 31/12/9999 no lo tiene.
    [Fact]
    public async Task GetShowingMovies_OnTheLastRepresentableDay_ThrowsBusinessRuleException()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetShowingMoviesAsync(genreId: null, DateOnly.MaxValue, Ct));
    }

    [Fact]
    public async Task Update_WhenItsGenreWasDeleted_CanStillBeEditedWithoutChangingIt()
    {
        var created = await _sut.CreateAsync(NewRequest(_drama.Id), Ct);
        _drama.IsActive = false;

        await _sut.UpdateAsync(created.Id, ToUpdate(NewRequest(_drama.Id)) with { Title = "Nuevo título" }, Ct);

        Assert.Equal("Nuevo título", Assert.Single(_movies.Stored).Title);
    }

    private async Task<Guid> CreateWithShowtimeAsync(DateTime start)
    {
        var movie = await _sut.CreateAsync(NewRequest(_drama.Id), Ct);
        _showtimes.Seed(new Showtime { Id = Guid.NewGuid(), MovieId = movie.Id, ScreenId = Guid.NewGuid(), StartTime = start, Price = 10m });
        return movie.Id;
    }

    private static CreateMovieRequest NewRequest(Guid genreId) =>
        new("Película", "Sinopsis", 120, genreId, "", new DateTime(2026, 1, 1), 10m);

    private static UpdateMovieRequest ToUpdate(CreateMovieRequest r) =>
        new(r.Title, r.Synopsis, r.DurationMinutes, r.GenreId, r.ImageUrl, r.ReleaseDate, r.SuggestedPrice, r.IsShowing);
}
