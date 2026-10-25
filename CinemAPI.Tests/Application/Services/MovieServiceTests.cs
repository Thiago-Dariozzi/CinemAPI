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
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly MovieService _sut;

    private readonly Genre _drama = new() { Id = Guid.NewGuid(), Name = "Drama" };

    public MovieServiceTests()
    {
        _genres.Seed(_drama);
        _sut = new MovieService(_movies, _genres, _unitOfWork);
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

        var listed = await _sut.GetShowingMoviesAsync(Ct);

        Assert.Equal(showing.Id, Assert.Single(listed).Id);
    }

    [Fact]
    public async Task Update_WhenItsGenreWasDeleted_CanStillBeEditedWithoutChangingIt()
    {
        var created = await _sut.CreateAsync(NewRequest(_drama.Id), Ct);
        _drama.IsActive = false;

        await _sut.UpdateAsync(created.Id, ToUpdate(NewRequest(_drama.Id)) with { Title = "Nuevo título" }, Ct);

        Assert.Equal("Nuevo título", Assert.Single(_movies.Stored).Title);
    }

    private static CreateMovieRequest NewRequest(Guid genreId) =>
        new("Película", "Sinopsis", 120, genreId, "", new DateTime(2026, 1, 1), 10m);

    private static UpdateMovieRequest ToUpdate(CreateMovieRequest r) =>
        new(r.Title, r.Synopsis, r.DurationMinutes, r.GenreId, r.ImageUrl, r.ReleaseDate, r.SuggestedPrice, r.IsShowing);
}
