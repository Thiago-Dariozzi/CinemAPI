using Application.DTOs.Genres;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Repositories;

namespace CinemAPI.Tests.Infrastructure;

// Cada test arma sus propios datos con Ids nuevos, así que comparten la base sin pisarse.
[Collection(SqlServerTestGroup.Name)]
public sealed class RepositoryIntegrationTests(SqlServerDatabaseFixture database)
{
    private static readonly DateTime Day = new(2026, 11, 1);

    [Fact]
    public async Task ShowtimeRepository_GetByScreenAndDate_ReturnsWholeDayOnly()
    {
        var (_, movie, screen) = await SeedCatalogAsync(durationMinutes: 120);
        var atMidnight = NewShowtime(movie, screen, Day);
        var lastMinute = NewShowtime(movie, screen, Day.AddHours(23).AddMinutes(59));
        var nextDay = NewShowtime(movie, screen, Day.AddDays(1));
        var previousDay = NewShowtime(movie, screen, Day.AddMinutes(-1));
        await SaveAsync(atMidnight, lastMinute, nextDay, previousDay);

        await using var context = database.CreateContext();
        var result = await new ShowtimeRepository(context).GetByScreenAndDateAsync(screen.Id, Day.AddHours(15), CancellationToken.None);

        Assert.Equal([atMidnight.Id, lastMinute.Id], result.Select(s => s.Id));
    }

    [Fact]
    public async Task ShowtimeRepository_GetOccupancy_UsesTimeWindow_AndKeepsDurationOfDeletedMovies()
    {
        var (_, movie, screen) = await SeedCatalogAsync(durationMinutes: 95);
        var (_, deletedMovie, _) = await SeedCatalogAsync(durationMinutes: 150);
        var previousNight = NewShowtime(movie, screen, Day.AddHours(-1));
        var orphan = NewShowtime(deletedMovie, screen, Day.AddHours(18));
        var outsideWindow = NewShowtime(movie, screen, Day.AddHours(22));
        await SaveAsync(previousNight, orphan, outsideWindow);
        await DeactivateAsync<Movie>(deletedMovie.Id);

        await using var context = database.CreateContext();
        var occupancy = await new ShowtimeRepository(context)
            .GetOccupancyByScreenAsync(screen.Id, Day.AddHours(-2), Day.AddHours(20), CancellationToken.None);

        Assert.Equal([previousNight.Id, orphan.Id], occupancy.Select(o => o.ShowtimeId));
        Assert.Equal((95, movie.Title), (occupancy[0].DurationMinutes, occupancy[0].MovieTitle));
        Assert.Equal((150, deletedMovie.Title), (occupancy[1].DurationMinutes, occupancy[1].MovieTitle));
    }

    [Fact]
    public async Task UserRepository_EmailExists_CountsDeletedUsers_AndCanExcludeOne()
    {
        var deleted = new User { Id = Guid.NewGuid(), Name = "Baja", Email = $"baja.{Guid.NewGuid():N}@cine.com", PasswordHash = "x", IsActive = false };
        await SaveAsync(deleted);

        await using var context = database.CreateContext();
        var repository = new UserRepository(context);

        Assert.True(await repository.EmailExistsAsync(deleted.Email.ToUpperInvariant(), excludeId: null, CancellationToken.None));
        Assert.False(await repository.EmailExistsAsync(deleted.Email, excludeId: deleted.Id, CancellationToken.None));
        Assert.False(await repository.EmailExistsAsync($"nadie.{Guid.NewGuid():N}@cine.com", excludeId: null, CancellationToken.None));
    }

    [Fact]
    public async Task QueryFilter_HidesInactiveEntities_FromGetAllAndGetById()
    {
        var (_, movie, screen) = await SeedCatalogAsync(durationMinutes: 100);
        var inactive = NewShowtime(movie, screen, Day.AddHours(10));
        inactive.IsActive = false;
        await SaveAsync(inactive);

        await using var context = database.CreateContext();
        var repository = new ShowtimeRepository(context);

        Assert.Null(await repository.GetByIdAsync(inactive.Id, CancellationToken.None));
        Assert.DoesNotContain(await repository.GetAllAsync(CancellationToken.None), s => s.Id == inactive.Id);
    }

    [Fact]
    public async Task GetById_ReturnsTrackedEntity_ThatUnitOfWorkSaves()
    {
        var (_, _, screen) = await SeedCatalogAsync(durationMinutes: 100);

        await using (var context = database.CreateContext())
        {
            var tracked = await new ScreenRepository(context).GetByIdAsync(screen.Id, CancellationToken.None);
            tracked!.Capacity = 321;
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        Assert.Equal(321, (await new ScreenRepository(verify).GetByIdAsync(screen.Id, CancellationToken.None))!.Capacity);
    }

    [Fact]
    public async Task MovieRepository_GetByTitleAndCountByGenre_IgnoreCaseAndInactive()
    {
        var (genre, movie, _) = await SeedCatalogAsync(durationMinutes: 100);
        var deleted = new Movie { Id = Guid.NewGuid(), Title = movie.Title, GenreId = genre.Id, DurationMinutes = 90, IsActive = false };
        await SaveAsync(deleted);

        await using var context = database.CreateContext();
        var repository = new MovieRepository(context);

        Assert.Equal([movie.Id], (await repository.GetByTitleAsync(movie.Title.ToUpperInvariant(), CancellationToken.None)).Select(m => m.Id));
        Assert.Equal(1, await repository.CountByGenreAsync(genre.Id, CancellationToken.None));
    }

    // GET /api/movie/active: solo las activas y en cartelera.
    [Fact]
    public async Task MovieRepository_GetShowingMovies_ExcludesNotShowingAndDeleted()
    {
        var (genre, showing, _) = await SeedCatalogAsync(durationMinutes: 100);
        var notShowing = new Movie { Id = Guid.NewGuid(), Title = "Fuera de cartelera", GenreId = genre.Id, DurationMinutes = 90, IsShowing = false };
        var deleted = new Movie { Id = Guid.NewGuid(), Title = "Dada de baja", GenreId = genre.Id, DurationMinutes = 90, IsActive = false };
        await SaveAsync(notShowing, deleted);

        await using var context = database.CreateContext();
        var ids = (await new MovieRepository(context).GetShowingMoviesAsync(CancellationToken.None)).Select(m => m.Id).ToList();

        Assert.Contains(showing.Id, ids);
        Assert.DoesNotContain(notShowing.Id, ids);
        Assert.DoesNotContain(deleted.Id, ids);
    }

    [Fact]
    public async Task SaveChanges_TranslatesUniqueIndexViolation_ToDomainException()
    {
        var name = $"Único {Guid.NewGuid():N}";
        await SaveAsync(new Genre { Id = Guid.NewGuid(), Name = name });

        await using var context = database.CreateContext();
        context.Genres.Add(new Genre { Id = Guid.NewGuid(), Name = name.ToUpperInvariant() });

        await Assert.ThrowsAsync<UniqueConstraintConflictException>(() => context.SaveChangesAsync());
    }

    // #45 de punta a punta: otro pedido inserta el mismo género entre la validación y el guardado.
    [Fact]
    public async Task GenreService_Create_WhenConcurrentInsertWinsTheRace_ThrowsGenreConflictException()
    {
        var name = $"Carrera {Guid.NewGuid():N}";
        var winner = new Genre { Id = Guid.NewGuid(), Name = name };

        await using var context = database.CreateContext();
        var raceBeforeSave = new RaceUnitOfWork(context, () => SaveAsync(winner));
        var sut = new GenreService(new GenreRepository(context), new MovieRepository(context), raceBeforeSave);

        var ex = await Assert.ThrowsAsync<GenreConflictException>(
            () => sut.CreateAsync(new CreateGenreRequest(name), CancellationToken.None));

        Assert.Equal($"Ya existe un género equivalente: '{name}' (Id {winner.Id}).", ex.Message);
    }

    [Fact]
    public async Task UserRepository_GetByEmail_IgnoresCase()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Ana", Email = $"ana.{Guid.NewGuid():N}@cine.com", PasswordHash = "x" };
        await SaveAsync(user);

        await using var context = database.CreateContext();
        var found = await new UserRepository(context).GetByEmailAsync(user.Email.ToUpperInvariant(), CancellationToken.None);

        Assert.Equal(user.Id, found?.Id);
    }

    private async Task<(Genre Genre, Movie Movie, Screen Screen)> SeedCatalogAsync(int durationMinutes)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var genre = new Genre { Id = Guid.NewGuid(), Name = $"Género {suffix}" };
        var movie = new Movie { Id = Guid.NewGuid(), Title = $"Película {suffix}", GenreId = genre.Id, DurationMinutes = durationMinutes };
        var screen = new Screen { Id = Guid.NewGuid(), Name = $"Sala {suffix}", Capacity = 100 };
        await SaveAsync(genre, movie, screen);
        return (genre, movie, screen);
    }

    private static Showtime NewShowtime(Movie movie, Screen screen, DateTime start) =>
        new() { Id = Guid.NewGuid(), MovieId = movie.Id, ScreenId = screen.Id, StartTime = start, Price = 10m };

    private async Task SaveAsync(params object[] entities)
    {
        await using var context = database.CreateContext();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    private async Task DeactivateAsync<T>(Guid id) where T : class, Domain.Interfaces.IEntity
    {
        await using var context = database.CreateContext();
        var entity = await context.Set<T>().FindAsync(id);
        entity!.IsActive = false;
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
