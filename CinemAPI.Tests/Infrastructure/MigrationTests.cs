using Domain.Entities;
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

    // Editada a mano: EF proponía borrar Password y crear PasswordHash vacía. El valor viejo tiene que
    // llegar intacto para que DataInitializer lo hashee.
    [Fact]
    public async Task RenameUserPasswordToPasswordHash_KeepsExistingPasswords()
    {
        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=CinemAPI_MigrationTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;";
        await using var context = new CinemAPIContext(
            new DbContextOptionsBuilder<CinemAPIContext>().UseSqlServer(connectionString).Options);
        var migrator = context.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync("AddMovieIsShowing");
            var userId = Guid.NewGuid();
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Users (Id, Name, Email, Password, Role, IsActive) VALUES ({userId}, N'Ana', N'ana@cine.com', N'clave-vieja', N'Client', 1)");

            await migrator.MigrateAsync();

            var passwordHash = await context.Database
                .SqlQuery<string>($"SELECT PasswordHash AS Value FROM Users WHERE Id = {userId}")
                .SingleAsync();
            Assert.Equal("clave-vieja", passwordHash);
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    // Los tickets viejos (película + sala + usuario, sin función ni butaca) pasan a compras con butaca.
    [Fact]
    public async Task AddSeatLayoutFeesAndPurchases_MigratesScreensAndExistingTickets()
    {
        var connectionString =
            $@"Server=(localdb)\MSSQLLocalDB;Database=CinemAPI_MigrationTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True;";
        await using var context = new CinemAPIContext(
            new DbContextOptionsBuilder<CinemAPIContext>().UseSqlServer(connectionString).Options);
        var migrator = context.GetService<IMigrator>();

        try
        {
            await migrator.MigrateAsync("RenameUserPasswordToPasswordHash");
            var genreId = Guid.NewGuid();
            var movieId = Guid.NewGuid();
            var tinyScreen = Guid.NewGuid();   // 2 lugares → 1 fila × 10 butacas
            var bigScreen = Guid.NewGuid();    // 25 lugares → 3 filas × 10
            var emptyScreen = Guid.NewGuid();  // sin funciones de esa película
            var userId = Guid.NewGuid();
            var earlyShowtime = Guid.NewGuid();
            var laterShowtime = Guid.NewGuid();
            var bigShowtime = Guid.NewGuid();
            await context.Database.ExecuteSqlAsync($"INSERT INTO Genres (Id, Name, IsActive) VALUES ({genreId}, N'Drama', 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Movies (Id, Title, DurationMinutes, GenreId, ImageUrl, ReleaseDate, IsActive, IsShowing) VALUES ({movieId}, N'Vieja', 100, {genreId}, N'', '2020-01-01', 1, 1)");
            await context.Database.ExecuteSqlAsync($"INSERT INTO Screens (Id, Name, Capacity, IsActive) VALUES ({tinyScreen}, N'Chica', 2, 1)");
            await context.Database.ExecuteSqlAsync($"INSERT INTO Screens (Id, Name, Capacity, IsActive) VALUES ({bigScreen}, N'Grande', 25, 1)");
            await context.Database.ExecuteSqlAsync($"INSERT INTO Screens (Id, Name, Capacity, IsActive) VALUES ({emptyScreen}, N'Vacía', 500, 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Users (Id, Name, Email, PasswordHash, Role, IsActive) VALUES ({userId}, N'Ana', N'ana@cine.com', N'x', N'Client', 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Showtimes (Id, MovieId, ScreenId, StartTime, Price, IsActive) VALUES ({earlyShowtime}, {movieId}, {tinyScreen}, '2026-10-01T18:00:00', 10, 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Showtimes (Id, MovieId, ScreenId, StartTime, Price, IsActive) VALUES ({laterShowtime}, {movieId}, {tinyScreen}, '2026-10-05T18:00:00', 10, 1)");
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO Showtimes (Id, MovieId, ScreenId, StartTime, Price, IsActive) VALUES ({bigShowtime}, {movieId}, {bigScreen}, '2026-10-05T18:00:00', 10, 1)");

            // Comprados el 03/10 en la sala chica: la primera función desde esa fecha es la del 05/10.
            var first = await InsertLegacyTicketAsync(context, movieId, tinyScreen, userId, "2026-10-03T10:00", 9.5m);
            var second = await InsertLegacyTicketAsync(context, movieId, tinyScreen, userId, "2026-10-03T11:00", 9.5m);
            // Con 8 más se llenan las 10 butacas de la sala chica; el undécimo ya no entra: se borra.
            for (var i = 0; i < 8; i++)
            {
                await InsertLegacyTicketAsync(context, movieId, tinyScreen, userId, "2026-10-03T12:00", 9.5m);
            }

            var overflow = await InsertLegacyTicketAsync(context, movieId, tinyScreen, userId, "2026-10-04T12:00", 9.5m);
            // Comprado después de todas las funciones: la más cercana.
            var late = await InsertLegacyTicketAsync(context, movieId, bigScreen, userId, "2026-10-20T10:00", 12m);
            // Sin ninguna función de esa película en esa sala: se borra.
            var orphan = await InsertLegacyTicketAsync(context, movieId, emptyScreen, userId, "2026-10-03T10:00", 8m);

            await migrator.MigrateAsync();

            var layouts = await context.Screens.IgnoreQueryFilters().OrderBy(s => s.Name)
                .Select(s => new { s.Name, s.Rows, s.SeatsPerRow }).ToListAsync();
            Assert.Equal(
                [new { Name = "Chica", Rows = 1, SeatsPerRow = 10 }, new { Name = "Grande", Rows = 3, SeatsPerRow = 10 }, new { Name = "Vacía", Rows = 26, SeatsPerRow = 20 }],
                layouts);

            var tickets = await context.Tickets.IgnoreQueryFilters().ToListAsync();
            Assert.Equal(11, tickets.Count);
            Assert.DoesNotContain(tickets, t => t.Id == overflow || t.Id == orphan);
            var byId = tickets.ToDictionary(t => t.Id);
            Assert.Equal((laterShowtime, 1, 1), (byId[first].ShowtimeId, byId[first].SeatRow, byId[first].SeatNumber));
            Assert.Equal((laterShowtime, 1, 2), (byId[second].ShowtimeId, byId[second].SeatRow, byId[second].SeatNumber));
            Assert.Equal((bigShowtime, 1, 1), (byId[late].ShowtimeId, byId[late].SeatRow, byId[late].SeatNumber));

            var purchases = await context.Purchases.IgnoreQueryFilters().ToListAsync();
            Assert.Equal(11, purchases.Count);
            Assert.All(tickets, t => Assert.Equal(t.Id, t.PurchaseId));
            var latePurchase = purchases.Single(p => p.Id == late);
            Assert.Equal((userId, new DateTime(2026, 10, 20, 10, 0, 0), 12m, 0m, 12m, PurchaseStatus.Confirmed),
                (latePurchase.UserId, latePurchase.CreatedAt, latePurchase.Subtotal, latePurchase.FeeAmount, latePurchase.Total, latePurchase.Status));
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<Guid> InsertLegacyTicketAsync(
        CinemAPIContext context, Guid movieId, Guid screenId, Guid userId, string buyDate, decimal price)
    {
        var id = Guid.NewGuid();
        var date = DateTime.Parse(buyDate, System.Globalization.CultureInfo.InvariantCulture);
        await context.Database.ExecuteSqlAsync(
            $"INSERT INTO Tickets (Id, MovieId, ScreenId, UserId, BuyDate, FinalPrice, IsActive) VALUES ({id}, {movieId}, {screenId}, {userId}, {date}, {price}, 1)");
        return id;
    }
}
