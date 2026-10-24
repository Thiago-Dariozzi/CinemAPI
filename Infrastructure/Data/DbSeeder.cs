using Bogus;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class DbSeeder
{
    private static readonly string[] GenreNames =
        { "Acción", "Comedia", "Drama", "Terror", "Ciencia Ficción", "Animación", "Romance", "Suspenso" };

    public static async Task SeedAsync(CinemAPIContext context)
    {
        // Si ya hay películas cargadas (aunque estén dadas de baja), asumimos que ya se sembró antes.
        var isFirstSeed = !await context.Movies.IgnoreQueryFilters().AnyAsync();

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
                .RuleFor(s => s.Capacity, f => f.Random.Int(20, 300))
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
                .RuleFor(u => u.Password, f => "Test1234!")
                .RuleFor(u => u.Role, f => f.PickRandom("Client", "Client", "Client", "Admin")) // ~25% admin
                .RuleFor(u => u.IsActive, f => true);
            var users = userFaker.Generate(30);

            await context.Genres.AddRangeAsync(genres);
            await context.Screens.AddRangeAsync(screens);
            await context.Movies.AddRangeAsync(movies);
            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // --- Tickets (dependen de los IDs ya generados arriba) ---
            var ticketFaker = new Faker<Ticket>("es")
                .RuleFor(t => t.Id, f => Guid.NewGuid())
                .RuleFor(t => t.MovieId, f => f.PickRandom(movies).Id)
                .RuleFor(t => t.ScreenId, f => f.PickRandom(screens).Id)
                .RuleFor(t => t.UserId, f => f.PickRandom(users).Id)
                .RuleFor(t => t.BuyDate, f => f.Date.Recent(60))
                .RuleFor(t => t.FinalPrice, f => f.Random.Decimal(5, 25))
                .RuleFor(t => t.IsActive, f => true);
            var tickets = ticketFaker.Generate(50);

            await context.Tickets.AddRangeAsync(tickets);
            await context.SaveChangesAsync();
        }
        else
        {
            screens = await context.Screens.ToListAsync();
            movies = await context.Movies.ToListAsync();
        }

        // --- Showtimes (funciones) ---
        if (!await context.Showtimes.IgnoreQueryFilters().AnyAsync() && movies.Count > 0 && screens.Count > 0)
        {
            var showtimes = GenerateShowtimes(movies, screens);
            await context.Showtimes.AddRangeAsync(showtimes);
            await context.SaveChangesAsync();
        }
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
                        StartTime = faker.Date.Soon(14).Date.AddHours(faker.Random.Int(12, 22)),
                        Price = faker.Random.Decimal(5, 25),
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
