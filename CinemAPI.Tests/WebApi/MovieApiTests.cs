using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CinemAPI.Tests.WebApi;

// Validaciones del alta de películas que dependen del model binding (no las cubren los tests del servicio).
[Collection(ApiTestGroup.Name)]
public sealed class MovieApiTests(ApiFixture api)
{
    [Fact]
    public async Task Create_WithReleaseDateAndImageUrl_SavesBoth()
    {
        using var asAdmin = api.CreateClient(api.AdminToken);
        var genreId = await CreateGenreAsync(asAdmin);

        var response = await asAdmin.PostAsJsonAsync("/api/movie", new
        {
            title = "Con estreno",
            durationMinutes = 100,
            genreId,
            imageUrl = "https://example.com/poster.jpg",
            releaseDate = "2026-03-15",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var movie = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new DateTime(2026, 3, 15), movie.RootElement.GetProperty("releaseDate").GetDateTime());
        Assert.Equal("https://example.com/poster.jpg", movie.RootElement.GetProperty("imageUrl").GetString());
    }

    // Bug: el formulario no mandaba la fecha y se guardaba 01/01/0001 ("Estreno: 1 de enero de 1").
    [Fact]
    public async Task Create_WithoutReleaseDate_Returns400()
    {
        using var asAdmin = api.CreateClient(api.AdminToken);
        var genreId = await CreateGenreAsync(asAdmin);

        var response = await asAdmin.PostAsJsonAsync("/api/movie", new { title = "Sin estreno", durationMinutes = 100, genreId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("La fecha de estreno es obligatoria.", await ErrorMessagesAsync(response));
    }

    [Theory]
    [InlineData("hola")]
    [InlineData("www.sin-protocolo.com/poster.jpg")]
    [InlineData("")]
    public async Task Create_WithAnInvalidImageUrl_Returns400(string imageUrl)
    {
        using var asAdmin = api.CreateClient(api.AdminToken);
        var genreId = await CreateGenreAsync(asAdmin);

        var response = await asAdmin.PostAsJsonAsync("/api/movie", new
        {
            title = "URL rota",
            durationMinutes = 100,
            genreId,
            imageUrl,
            releaseDate = "2026-03-15",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("La URL de la imagen no es válida.", await ErrorMessagesAsync(response));
    }

    [Fact]
    public async Task Create_WithoutImageUrl_IsAllowed()
    {
        using var asAdmin = api.CreateClient(api.AdminToken);
        var genreId = await CreateGenreAsync(asAdmin);

        var response = await asAdmin.PostAsJsonAsync("/api/movie", new { title = "Sin póster", durationMinutes = 100, genreId, releaseDate = "2026-03-15" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // Un precio sugerido de 0 se mostraba como "Sugerido: $0"; sin precio de referencia va vacío (null).
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Create_WithASuggestedPriceOfZeroOrLess_Returns400(int suggestedPrice)
    {
        using var asAdmin = api.CreateClient(api.AdminToken);
        var genreId = await CreateGenreAsync(asAdmin);

        var response = await asAdmin.PostAsJsonAsync("/api/movie", new
        {
            title = "Precio cero",
            durationMinutes = 100,
            genreId,
            releaseDate = "2026-03-15",
            suggestedPrice,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("El precio sugerido debe ser mayor a 0 y no superar 1.000.000.", await ErrorMessagesAsync(response));
    }

    private static async Task<Guid> CreateGenreAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/genre", new { name = $"Género {Guid.NewGuid():N}"[..20] });
        response.EnsureSuccessStatusCode();
        using var genre = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return genre.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<List<string>> ErrorMessagesAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var messages = new List<string>();
        foreach (var field in problem.RootElement.GetProperty("errors").EnumerateObject())
        {
            foreach (var message in field.Value.EnumerateArray())
            {
                messages.Add(message.GetString() ?? string.Empty);
            }
        }
        return messages;
    }
}
