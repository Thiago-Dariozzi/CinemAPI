using Application.Interfaces;
using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public static class DbSeeder
{
    private static readonly string[] GenreNames =
        { "Acción", "Comedia", "Drama", "Terror", "Ciencia Ficción", "Animación", "Romance", "Suspenso" };

    public static async Task SeedAsync(
        CinemAPIContext context, IPasswordHasher hasher, IConfiguration configuration, ILogger logger, CancellationToken ct)
    {
        // Un hash para todos los usuarios de prueba: calcular PBKDF2 por cada uno hace lento el arranque.
        var demoPassword = configuration["DemoUsers:Password"];
        var demoPasswordHash = string.IsNullOrEmpty(demoPassword) ? null : hasher.Hash(demoPassword);
        if (demoPasswordHash is null)
        {
            logger.DemoPasswordMissing();
        }

        // Si ya hay películas cargadas (aunque estén dadas de baja), asumimos que ya se sembró antes.
        var isFirstSeed = !await context.Movies.IgnoreQueryFilters().AnyAsync(ct);

        List<Screen> screens;
        List<Movie> movies;

        if (isFirstSeed)
        {
            // --- Genres ---
            var genres = GenreNames
                .Select(name => new Genre { Id = Guid.NewGuid(), Name = name, IsActive = true })
                .ToList();

            // --- Screens ---
            var screenFaker = new Faker<Screen>("es")
                .RuleFor(s => s.Id, f => Guid.NewGuid())
                .RuleFor(s => s.Name, f => $"Sala {f.IndexFaker + 1}")
                .RuleFor(s => s.Rows, f => f.Random.Int(6, 15))
                .RuleFor(s => s.SeatsPerRow, f => f.Random.Int(8, 20))
                .RuleFor(s => s.IsActive, f => true);
            screens = screenFaker.Generate(20);

            // --- Movies ---
            var movieFaker = new Faker<Movie>("es")
                .RuleFor(m => m.Id, f => Guid.NewGuid())
                .RuleFor(m => m.Title, f => f.Commerce.ProductName())
                .RuleFor(m => m.Synopsis, f => f.Lorem.Paragraph(3))
                .RuleFor(m => m.DurationMinutes, f => f.Random.Int(80, 180))
                .RuleFor(m => m.GenreId, f => f.PickRandom(genres).Id)
                .RuleFor(m => m.ImageUrl, f => f.Image.PicsumUrl())
                .RuleFor(m => m.ReleaseDate, f => f.Date.Past(3))
                .RuleFor(m => m.IsActive, f => true);
            movies = movieFaker.Generate(30);

            // --- Users ---
            var userFaker = new Faker<User>("es")
                .RuleFor(u => u.Id, f => Guid.NewGuid())
                .RuleFor(u => u.Name, f => f.Name.FullName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Name))
                .RuleFor(u => u.PasswordHash, f => demoPasswordHash ?? hasher.Hash(Guid.NewGuid().ToString()))
                .RuleFor(u => u.Role, f => Roles.Client)
                .RuleFor(u => u.IsActive, f => true);
            var users = userFaker.Generate(30);

            await context.Genres.AddRangeAsync(genres, ct);
            await context.Screens.AddRangeAsync(screens, ct);
            await context.Movies.AddRangeAsync(movies, ct);
            await context.Users.AddRangeAsync(users, ct);
            await context.SaveChangesAsync(ct);
        }
        else
        {
            screens = await context.Screens.ToListAsync(ct);
            movies = await context.Movies.ToListAsync(ct);
        }

        if (demoPasswordHash is not null)
        {
            await EnsureDemoUserAsync(context, "admin@cinemapi.com", "Admin Demo", Roles.Admin, demoPasswordHash, ct);
            await EnsureDemoUserAsync(context, "cliente@cinemapi.com", "Cliente Demo", Roles.Client, demoPasswordHash, ct);
        }

        // --- Showtimes (funciones) ---
        // Si no queda ninguna función por delante (base nueva o con funciones vencidas) se generan nuevas:
        // sin eso no hay nada para comprar.
        if (!await context.Showtimes.AnyAsync(s => s.StartTime > DateTime.Now, ct) && movies.Count > 0 && screens.Count > 0)
        {
            var showtimes = GenerateShowtimes(movies, screens);
            await context.Showtimes.AddRangeAsync(showtimes, ct);
            await context.SaveChangesAsync(ct);
        }

        await SeedFeeAsync(context, ct);
        await SeedPurchasesAsync(context, ct);
    }

    // Un cargo vigente de ejemplo para que el resumen de compra muestre subtotal + cargo + total.
    private static async Task SeedFeeAsync(CinemAPIContext context, CancellationToken ct)
    {
        if (await context.Fees.IgnoreQueryFilters().AnyAsync(ct))
        {
            return;
        }

        context.Fees.Add(new Fee { Id = Guid.NewGuid(), Name = "Cargo por servicio", Percentage = 10m, FixedAmount = 0m, IsCurrent = true, IsActive = true });
        await context.SaveChangesAsync(ct);
    }

    // Compras de clientes al azar en funciones próximas para que los mapas de butacas tengan lugares ocupados y Ventas tenga
    // datos. Cada compra toma butacas libres de su función y respeta el cálculo de PurchaseService.
    private static async Task SeedPurchasesAsync(CinemAPIContext context, CancellationToken ct)
    {
        const int PurchaseCount = 60;

        var upcoming = context.Showtimes.Where(s => s.StartTime > DateTime.Now);
        if (await context.Purchases.AnyAsync(p => upcoming.Any(s => s.Id == p.ShowtimeId), ct))
        {
            return;
        }

        var clients = await context.Users.Where(u => u.Role == Roles.Client).Select(u => u.Id).ToListAsync(ct);
        var showtimes = await upcoming.ToListAsync(ct);
        var screens = await context.Screens.ToDictionaryAsync(s => s.Id, ct);
        var fee = await context.Fees.FirstOrDefaultAsync(f => f.IsCurrent, ct);
        showtimes = showtimes.Where(s => screens.ContainsKey(s.ScreenId)).ToList();
        if (clients.Count == 0 || showtimes.Count == 0)
        {
            return;
        }

        var faker = new Faker("es");
        var taken = new HashSet<(Guid ShowtimeId, int Row, int Number)>();
        for (var i = 0; i < PurchaseCount; i++)
        {
            var showtime = faker.PickRandom(showtimes);
            var screen = screens[showtime.ScreenId];
            var row = faker.Random.Int(1, screen.Rows);
            var first = faker.Random.Int(1, screen.SeatsPerRow);
            var seats = Enumerable.Range(first, faker.Random.Int(1, 4))
                .Where(n => n <= screen.SeatsPerRow && taken.Add((showtime.Id, row, n)))
                .ToList();
            if (seats.Count == 0)
            {
                continue;
            }

            var subtotal = showtime.Price * seats.Count;
            var feeAmount = fee?.CalculateFor(subtotal) ?? 0m;
            var purchaseId = Guid.NewGuid();
            context.Purchases.Add(new Purchase
            {
                Id = purchaseId,
                UserId = faker.PickRandom(clients),
                ShowtimeId = showtime.Id,
                CreatedAt = DateTime.Now.AddDays(-faker.Random.Int(0, 10)).AddMinutes(-faker.Random.Int(0, 600)),
                Subtotal = subtotal,
                FeeId = fee?.Id,
                FeeAmount = feeAmount,
                Total = subtotal + feeAmount,
                Status = PurchaseStatus.Confirmed,
                Tickets = seats.Select(n => new Ticket
                {
                    Id = Guid.NewGuid(),
                    PurchaseId = purchaseId,
                    ShowtimeId = showtime.Id,
                    SeatRow = row,
                    SeatNumber = n,
                    Price = showtime.Price,
                }).ToList(),
            });
        }

        await context.SaveChangesAsync(ct);
    }

    // Un usuario demo por rol de staff y uno cliente (el SuperAdmin sale de SeedSuperAdmin). Si ya existe
    // no se toca, así una contraseña cambiada a mano no se pisa en cada arranque.
    private static async Task EnsureDemoUserAsync(
        CinemAPIContext context, string email, string name, string role, string passwordHash, CancellationToken ct)
    {
        if (await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct))
        {
            return;
        }

        context.Users.Add(new User { Id = Guid.NewGuid(), Name = name, Email = email, Role = role, PasswordHash = passwordHash, IsActive = true });
        await context.SaveChangesAsync(ct);
    }

    // 3 funciones por película en salas y horarios al azar, descartando las que se superpondrían
    // con otra ya generada en la misma sala: el seeder respeta la misma regla que ShowtimeService.
    private static List<Showtime> GenerateShowtimes(List<Movie> movies, List<Screen> screens)
    {
        const int ShowtimesPerMovie = 3;
        const int MaxAttemptsPerShowtime = 20;

        var faker = new Faker("es");
        var durations = movies.ToDictionary(m => m.Id, m => m.DurationMinutes + Showtime.CleaningBufferMinutes);
        var showtimes = new List<Showtime>();

        foreach (var movie in movies)
        {
            for (var i = 0; i < ShowtimesPerMovie; i++)
            {
                for (var attempt = 0; attempt < MaxAttemptsPerShowtime; attempt++)
                {
                    var candidate = new Showtime
                    {
                        Id = Guid.NewGuid(),
                        MovieId = movie.Id,
                        ScreenId = faker.PickRandom(screens).Id,
                        StartTime = DateTime.Today.AddDays(faker.Random.Int(1, 14)).AddHours(faker.Random.Int(12, 22)),
                        Price = Math.Round(faker.Random.Decimal(5, 25), 2),
                        IsActive = true,
                    };
                    var candidateEnd = candidate.StartTime.AddMinutes(durations[movie.Id]);

                    var overlaps = showtimes.Any(s =>
                        s.ScreenId == candidate.ScreenId &&
                        candidate.StartTime < s.StartTime.AddMinutes(durations[s.MovieId]) &&
                        s.StartTime < candidateEnd);
                    if (!overlaps)
                    {
                        showtimes.Add(candidate);
                        break;
                    }
                }
            }
        }

        return showtimes;
    }
}
