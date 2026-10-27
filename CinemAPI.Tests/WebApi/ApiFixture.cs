using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CinemAPI.Tests.Infrastructure;
using Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WebApi.Controllers;

namespace CinemAPI.Tests.WebApi;

// La API completa en memoria (pipeline real: autenticación, políticas, ProblemDetails) contra una base
// LocalDB propia. Ambiente "Testing": corre el rehash y el SuperAdmin inicial, pero no los datos de Bogus.
public sealed class ApiFixture : IAsyncLifetime
{
    public const string SuperAdminEmail = "root@cine.test";
    public const string SuperAdminPassword = "Root-12345";
    public const string DefaultPassword = "Clave-12345";

    private readonly SqlServerDatabaseFixture _database = new();
    private WebApplicationFactory<AuthController> _factory = null!;

    public SqlServerDatabaseFixture Database => _database;
    public IServiceProvider Services => _factory.Services;

    public string SuperAdminToken { get; private set; } = string.Empty;
    public string AdminToken { get; private set; } = string.Empty;
    public string ClientToken { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();

        // UseSetting (y no ConfigureAppConfiguration): Program lee la configuración antes de Build().
        _factory = new WebApplicationFactory<AuthController>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Testing")
            .UseSetting("ConnectionStrings:CinemaConnection", _database.ConnectionString)
            .UseSetting("Jwt:Key", "clave-de-pruebas-de-al-menos-32-bytes!!")
            .UseSetting("SeedSuperAdmin:Email", SuperAdminEmail)
            .UseSetting("SeedSuperAdmin:Password", SuperAdminPassword));

        SuperAdminToken = await LoginAsync(SuperAdminEmail, SuperAdminPassword);

        var adminEmail = UniqueEmail("admin");
        using var asSuperAdmin = CreateClient(SuperAdminToken);
        var created = await asSuperAdmin.PostAsJsonAsync("/api/user", new { name = "Admin Test", email = adminEmail, password = DefaultPassword, role = "Admin" });
        created.EnsureSuccessStatusCode();
        AdminToken = await LoginAsync(adminEmail, DefaultPassword);

        ClientToken = (await RegisterClientAsync()).Token;
    }

    public HttpClient CreateClient(string? token = null)
    {
        var client = _factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    public string TokenFor(string role) => role switch
    {
        "SuperAdmin" => SuperAdminToken,
        "Admin" => AdminToken,
        "Client" => ClientToken,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    public async Task<string> LoginAsync(string email, string password)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        return await ReadTokenAsync(response);
    }

    public async Task<(string Token, Guid Id)> RegisterClientAsync()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { name = "Cliente Test", email = UniqueEmail("cliente"), password = DefaultPassword });
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (body.RootElement.GetProperty("token").GetString()!, body.RootElement.GetProperty("user").GetProperty("id").GetGuid());
    }

    // Una función lista para vender (película en cartelera, sala de rows × seatsPerRow), cargada directo
    // en la base para no depender de los endpoints del catálogo.
    public async Task<Guid> CreateShowtimeAsync(DateTime startTime, int rows = 5, int seatsPerRow = 10, decimal price = 10m)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var genre = new Genre { Id = Guid.NewGuid(), Name = $"Género {suffix}" };
        var movie = new Movie { Id = Guid.NewGuid(), Title = $"Película {suffix}", GenreId = genre.Id, DurationMinutes = 100, IsShowing = true };
        var screen = new Screen { Id = Guid.NewGuid(), Name = $"Sala {suffix}", Rows = rows, SeatsPerRow = seatsPerRow };
        var showtime = new Showtime { Id = Guid.NewGuid(), MovieId = movie.Id, ScreenId = screen.Id, StartTime = startTime, Price = price };
        await using var context = _database.CreateContext();
        context.AddRange(genre, movie, screen, showtime);
        await context.SaveChangesAsync();
        return showtime.Id;
    }

    public static string UniqueEmail(string prefix) => $"{prefix}.{Guid.NewGuid():N}@cine.test";

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }

    private static async Task<string> ReadTokenAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("token").GetString()!;
    }
}

[CollectionDefinition(Name)]
public sealed class ApiTestGroup : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}
