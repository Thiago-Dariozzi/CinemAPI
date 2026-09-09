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
        // Si ya hay películas cargadas, asumimos que ya se sembró antes.
        var isFirstSeed = !await context.Movies.AnyAsync();

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
        if (!await context.Showtimes.AnyAsync() && movies.Count > 0 && screens.Count > 0)
        {
            var showtimes = new List<Showtime>();
            foreach (var movie in movies)
            {
                var showtimesForMovie = new Faker<Showtime>("es")
                    .RuleFor(s => s.Id, f => Guid.NewGuid())
                    .RuleFor(s => s.MovieId, f => movie.Id)
                    .RuleFor(s => s.ScreenId, f => f.PickRandom(screens).Id)
                    .RuleFor(s => s.StartTime, f => f.Date.Soon(14).Date.AddHours(f.Random.Int(12, 22)))
                    .RuleFor(s => s.Price, f => f.Random.Decimal(5, 25))
                    .RuleFor(s => s.IsActive, f => true)
                    .Generate(3);
                showtimes.AddRange(showtimesForMovie);
            }

            await context.Showtimes.AddRangeAsync(showtimes);
            await context.SaveChangesAsync();
        }
    }
}
