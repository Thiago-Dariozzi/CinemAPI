using Application.DTOs.Genres;
using Application.Services;
using CinemAPI.Tests.Fakes;
using Domain.Entities;
using Domain.Exceptions;

namespace CinemAPI.Tests.Application.Services;

public sealed class GenreServiceTests
{
    private readonly FakeGenreRepository _genres = new();
    private readonly FakeMovieRepository _movies = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly GenreService _sut;

    private readonly Genre _action = new() { Id = Guid.NewGuid(), Name = "Acción" };

    public GenreServiceTests()
    {
        _genres.Seed(_action);
        _sut = new GenreService(_genres, _movies, _unitOfWork);
    }

    [Theory]
    [InlineData("  drama  ", "Drama")]
    [InlineData("ciencia ficción", "Ciencia ficción")]
    [InlineData("Ciencia Ficción", "Ciencia Ficción")] // no toca el resto del nombre
    public async Task Create_NormalizesName_TrimAndFirstLetterUppercase(string input, string expected)
    {
        var created = await _sut.CreateAsync(new CreateGenreRequest(input), CancellationToken.None);

        Assert.Equal(expected, created.Name);
    }

    [Theory]
    [InlineData("Acción")]
    [InlineData("acción")]
    [InlineData("ACCIÓN")]
    [InlineData("Accion")]
    [InlineData("ACCION")]
    [InlineData("  accion ")]
    public async Task Create_WhenEquivalentNameExists_ThrowsGenreConflictException(string name)
    {
        await Assert.ThrowsAsync<GenreConflictException>(() => _sut.CreateAsync(new CreateGenreRequest(name), CancellationToken.None));
    }

    // Documenta la semántica elegida (#19 de la auditoría): IgnoreNonSpace trata la ñ como n con
    // tilde, así que "Año" y "Ano" son el mismo género. La collation de la base
    // (Modern_Spanish_CI_AI) los consideraría distintos; por eso la comparación queda en el servicio.
    [Fact]
    public async Task Create_WhenNameDiffersOnlyByEnye_ThrowsGenreConflictException()
    {
        _genres.Seed(new Genre { Id = Guid.NewGuid(), Name = "Año" });

        await Assert.ThrowsAsync<GenreConflictException>(() => _sut.CreateAsync(new CreateGenreRequest("Ano"), CancellationToken.None));
    }

    [Fact]
    public async Task Create_WhenEquivalentGenreIsInactive_Allows()
    {
        _action.IsActive = false;

        var created = await _sut.CreateAsync(new CreateGenreRequest("accion"), CancellationToken.None);

        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_GeneratesIdAndActivates()
    {
        var created = await _sut.CreateAsync(new CreateGenreRequest("Terror"), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Update_KeepingItsOwnName_DoesNotConflictWithItself()
    {
        await _sut.UpdateAsync(_action.Id, new UpdateGenreRequest("acción"), CancellationToken.None);

        Assert.Equal("Acción", Assert.Single(_genres.Stored).Name);
    }

    [Fact]
    public async Task Update_ToNameOfAnotherGenre_ThrowsGenreConflictException()
    {
        var drama = new Genre { Id = Guid.NewGuid(), Name = "Drama" };
        _genres.Seed(drama);

        await Assert.ThrowsAsync<GenreConflictException>(
            () => _sut.UpdateAsync(drama.Id, new UpdateGenreRequest("ACCION"), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_WhenGenreHasActiveMovies_ThrowsAndKeepsItActive()
    {
        _movies.Seed(new Movie { Id = Guid.NewGuid(), Title = "Duro de matar", GenreId = _action.Id });

        await Assert.ThrowsAsync<GenreConflictException>(() => _sut.DeleteAsync(_action.Id, CancellationToken.None));
        Assert.True(_action.IsActive);
    }

    [Fact]
    public async Task Delete_WhenGenreOnlyHasInactiveMovies_SetsInactive()
    {
        _movies.Seed(new Movie { Id = Guid.NewGuid(), Title = "Borrada", GenreId = _action.Id, IsActive = false });

        await _sut.DeleteAsync(_action.Id, CancellationToken.None);

        Assert.False(Assert.Single(_genres.Stored).IsActive);
    }

    [Fact]
    public async Task GetById_WhenGenreDoesNotExist_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenGenreDoesNotExist_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), new UpdateGenreRequest("Drama"), CancellationToken.None));
    }

    [Fact]
    public async Task Delete_WhenGenreIsInactive_ThrowsNotFoundException()
    {
        _action.IsActive = false;

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(_action.Id, CancellationToken.None));
    }
}
