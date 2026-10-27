using Application.DTOs.Genres;
using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Exceptions;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

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

    // IgnoreQueryFilters (para las películas dadas de baja) apaga los filtros de toda la consulta: una
    // función borrada no puede seguir ocupando la sala.
    [Fact]
    public async Task ShowtimeRepository_GetOccupancy_IgnoresDeletedShowtimes()
    {
        var (_, movie, screen) = await SeedCatalogAsync(durationMinutes: 100);
        var deleted = NewShowtime(movie, screen, Day.AddHours(10));
        await SaveAsync(deleted);
        await DeactivateAsync<Showtime>(deleted.Id);

        await using var context = database.CreateContext();
        var occupancy = await new ShowtimeRepository(context)
            .GetOccupancyByScreenAsync(screen.Id, Day, Day.AddDays(1), CancellationToken.None);

        Assert.Empty(occupancy);
    }

    [Fact]
    public async Task ShowtimeRepository_HasUpcomingByScreen_IgnoresPastAndDeletedShowtimes()
    {
        var (_, movie, screen) = await SeedCatalogAsync(durationMinutes: 100);
        var past = NewShowtime(movie, screen, Day.AddHours(10));
        var deleted = NewShowtime(movie, screen, Day.AddDays(1));
        await SaveAsync(past, deleted);
        await DeactivateAsync<Showtime>(deleted.Id);

        await using var context = database.CreateContext();
        var repository = new ShowtimeRepository(context);

        Assert.False(await repository.HasUpcomingByScreenAsync(screen.Id, Day.AddHours(10), CancellationToken.None));
        Assert.True(await repository.HasUpcomingByScreenAsync(screen.Id, Day.AddHours(9), CancellationToken.None));
    }

    // El índice único filtrado de IsCurrent se verifica por sentencia y EF no ordena los UPDATE según él: en
    // un solo SaveChanges este test fallaba con 409. El servicio desmarca primero al vigente anterior.
    [Fact]
    public async Task FeeService_SwitchingTheCurrentFee_UnmarksThePreviousOneFirst()
    {
        try
        {
            Guid firstId, secondId;
            await using (var context = database.CreateContext())
            {
                var service = new FeeService(new FeeRepository(context), context);
                firstId = (await service.CreateAsync(new("Primero", 10m, 0m, IsCurrent: true), CancellationToken.None)).Id;
                secondId = (await service.CreateAsync(new("Segundo", 5m, 0m, IsCurrent: true), CancellationToken.None)).Id;
            }

            await using (var context = database.CreateContext())
            {
                var service = new FeeService(new FeeRepository(context), context);
                Assert.Equal(secondId, (await service.GetCurrentAsync(CancellationToken.None))!.Id);
                await service.UpdateAsync(firstId, new("Primero", 10m, 0m, IsCurrent: true), CancellationToken.None);
            }

            await using var check = database.CreateContext();
            Assert.Equal([firstId], await check.Fees.Where(f => f.IsCurrent).Select(f => f.Id).ToListAsync());
        }
        finally
        {
            // Los otros tests de esta base compran sin cargo.
            await using var cleanup = database.CreateContext();
            await cleanup.Fees.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(f => f.IsCurrent, false));
        }
    }

    // Si falla el segundo guardado (el que marca al nuevo), la transacción deshace también el primero: el
    // vigente anterior sigue vigente y el cargo nuevo no queda guardado.
    [Fact]
    public async Task FeeService_WhenTheSecondSaveFails_RollsBackAndKeepsThePreviousFee()
    {
        try
        {
            Guid previousId;
            await using (var context = database.CreateContext())
            {
                var service = new FeeService(new FeeRepository(context), context);
                previousId = (await service.CreateAsync(new("Anterior", 10m, 0m, IsCurrent: true), CancellationToken.None)).Id;
            }

            await using (var context = database.CreateContext())
            {
                var service = new FeeService(new FeeRepository(context), new FailingSecondSaveUnitOfWork(context));
                await Assert.ThrowsAsync<FeeConflictException>(
                    () => service.CreateAsync(new("Nuevo", 5m, 0m, IsCurrent: true), CancellationToken.None));
            }

            await using var check = database.CreateContext();
            Assert.Equal([previousId], await check.Fees.Where(f => f.IsCurrent).Select(f => f.Id).ToListAsync());
            Assert.False(await check.Fees.IgnoreQueryFilters().AnyAsync(f => f.Name == "Nuevo"));
        }
        finally
        {
            await using var cleanup = database.CreateContext();
            await cleanup.Fees.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(f => f.IsCurrent, false));
        }
    }

    [Fact]
    public async Task UserRepository_GetByRoles_SearchesByNameOrEmail_AndIncludesDeletedUsers()
    {
        // Letras en minúscula para buscar en mayúscula: la collation de la base no las distingue.
        var tag = string.Concat(Guid.NewGuid().ToString("N").Select(c => (char)('a' + c % 26)))[..10];
        var byName = new User { Id = Guid.NewGuid(), Name = $"Ana {tag}", Email = $"ana.{Guid.NewGuid():N}@cine.com", PasswordHash = "x", Role = Roles.Client };
        var byEmail = new User { Id = Guid.NewGuid(), Name = "Beto", Email = $"{tag}@cine.com", PasswordHash = "x", Role = Roles.Client, IsActive = false };
        var admin = new User { Id = Guid.NewGuid(), Name = $"Admin {tag}", Email = $"admin.{Guid.NewGuid():N}@cine.com", PasswordHash = "x", Role = Roles.Admin };
        await SaveAsync(byName, byEmail, admin);

        await using var context = database.CreateContext();
        var found = await new UserRepository(context).GetByRolesAsync([Roles.Client], tag.ToUpperInvariant(), CancellationToken.None);

        // Ordenados por nombre: "Ana …" antes que "Beto". El Admin no entra por el rol.
        Assert.Equal([byName.Id, byEmail.Id], found.Select(u => u.Id));
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
            tracked!.Rows = 21;
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        Assert.Equal(21, (await new ScreenRepository(verify).GetByIdAsync(screen.Id, CancellationToken.None))!.Rows);
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
        var screen = new Screen { Id = Guid.NewGuid(), Name = $"Sala {suffix}", Rows = 10, SeatsPerRow = 10 };
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

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) =>
            inner.ExecuteInTransactionAsync(work, cancellationToken);
    }

    // Guarda de verdad la primera vez y simula que la base rechaza la segunda (como si otro pedido hubiera
    // marcado otro cargo en el medio). La transacción es la real del contexto.
    private sealed class FailingSecondSaveUnitOfWork(IUnitOfWork inner) : IUnitOfWork
    {
        private int _saves;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            ++_saves == 2
                ? throw new UniqueConstraintConflictException(new InvalidOperationException("Simulada"))
                : inner.SaveChangesAsync(cancellationToken);

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken) =>
            inner.ExecuteInTransactionAsync(work, cancellationToken);
    }
}
