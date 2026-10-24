using Domain.Entities;
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
        var result = await new ShowtimeRepository(context).GetByScreenAndDate(screen.Id, Day.AddHours(15));

        Assert.Equal([atMidnight.Id, lastMinute.Id], result.Select(s => s.Id));
    }

    [Fact]
    public async Task ShowtimeRepository_GetOccupancy_IncludesDurationAndTitle_AndNullWhenMovieInactive()
    {
        var (_, movie, screen) = await SeedCatalogAsync(durationMinutes: 95);
        var (_, deletedMovie, _) = await SeedCatalogAsync(durationMinutes: 150);
        var showtime = NewShowtime(movie, screen, Day.AddHours(14));
        var orphan = NewShowtime(deletedMovie, screen, Day.AddHours(18));
        await SaveAsync(showtime, orphan);
        await DeactivateAsync<Movie>(deletedMovie.Id);

        await using var context = database.CreateContext();
        var occupancy = (await new ShowtimeRepository(context).GetOccupancyByScreenAndDate(screen.Id, Day)).ToList();

        Assert.Equal(2, occupancy.Count);
        Assert.Equal((95, movie.Title), (occupancy[0].DurationMinutes!.Value, occupancy[0].MovieTitle));
        Assert.Null(occupancy[1].DurationMinutes);
        Assert.Null(occupancy[1].MovieTitle);
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

        Assert.Null(await repository.GetById(inactive.Id));
        Assert.DoesNotContain(await repository.GetAll(), s => s.Id == inactive.Id);
    }

    [Fact]
    public async Task GetById_ReturnsTrackedEntity_ThatUnitOfWorkSaves()
    {
        var (_, _, screen) = await SeedCatalogAsync(durationMinutes: 100);

        await using (var context = database.CreateContext())
        {
            var tracked = await new ScreenRepository(context).GetById(screen.Id);
            tracked!.Capacity = 321;
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        Assert.Equal(321, (await new ScreenRepository(verify).GetById(screen.Id))!.Capacity);
    }

    [Fact]
    public async Task MovieRepository_GetByTitleAndCountByGenre_IgnoreCaseAndInactive()
    {
        var (genre, movie, _) = await SeedCatalogAsync(durationMinutes: 100);
        var deleted = new Movie { Id = Guid.NewGuid(), Title = movie.Title, GenreId = genre.Id, DurationMinutes = 90, IsActive = false };
        await SaveAsync(deleted);

        await using var context = database.CreateContext();
        var repository = new MovieRepository(context);

        Assert.Equal([movie.Id], (await repository.GetByTitle(movie.Title.ToUpperInvariant())).Select(m => m.Id));
        Assert.Equal(1, await repository.CountByGenre(genre.Id));
    }

    [Fact]
    public async Task UserRepository_GetByEmail_IgnoresCase()
    {
        var user = new User { Id = Guid.NewGuid(), Name = "Ana", Email = $"ana.{Guid.NewGuid():N}@cine.com", Password = "x" };
        await SaveAsync(user);

        await using var context = database.CreateContext();
        var found = await new UserRepository(context).GetByEmail(user.Email.ToUpperInvariant());

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
}
