using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CinemAPI.Tests.Infrastructure;

// Base propia (no la compartida del fixture): necesita estar en una migración anterior.
public sealed class MigrationTests
{
    [Fact]
    public async Task AddMovieIsShowing_LeavesExistingMoviesShowing()
    {
        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=CinemAPI_MigrationTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;";
        await using var context = new CinemAPIContext(
            new DbContextOptionsBuilder<CinemAPIContext>().UseSqlServer(connectionString).Options);
        var migrator = context.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync("AddShowtimeScreenStartTimeIndex");
            var genreId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Genres (Id, Name, IsActive) VALUES ({genreId}, N'Drama', 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Movies (Id, Title, DurationMinutes, GenreId, ImageUrl, ReleaseDate, IsActive) VALUES ({movieId}, N'Vieja', 100, {genreId}, N'', '2020-01-01', 1)");

            await migrator.MigrateAsync();

            var isShowing = await context.Database
                .SqlQuery<bool>($"SELECT IsShowing AS Value FROM Movies WHERE Id = {movieId}")
                .SingleAsync();
            Assert.True(isShowing);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }
}
