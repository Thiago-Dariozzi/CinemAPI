using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CinemAPI.Tests.WebApi;

// Autorización de punta a punta: token real, políticas por rol y la forma de los errores. Al menos un
// endpoint protegido (401 / 403 / permitido) y uno público por controller.
[Collection(ApiTestGroup.Name)]
public sealed class AuthorizationTests(ApiFixture api)
{
    private const string Anonymous = "Anonymous";
    private static readonly string NoSuchId = Guid.NewGuid().ToString();

    public static TheoryData<string, string, string, string?, HttpStatusCode> Cases => new()
    {
        // Auth
        { Anonymous, "GET", "/api/auth/me", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/auth/me", null, HttpStatusCode.OK },
        { "Admin", "GET", "/api/auth/me", null, HttpStatusCode.OK },

        // User: solo staff; Admin no ve Admins
        { Anonymous, "GET", "/api/user", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/user", null, HttpStatusCode.Forbidden },
        { "Admin", "GET", "/api/user", null, HttpStatusCode.OK },
        { "Admin", "GET", "/api/user?role=Admin", null, HttpStatusCode.Forbidden },
        { "SuperAdmin", "GET", "/api/user?role=Admin", null, HttpStatusCode.OK },

        // Genre: lectura pública, escritura staff
        { Anonymous, "GET", "/api/genre", null, HttpStatusCode.OK },
        { Anonymous, "POST", "/api/genre", """{"name":"{unique}"}""", HttpStatusCode.Unauthorized },
        { "Client", "POST", "/api/genre", """{"name":"{unique}"}""", HttpStatusCode.Forbidden },
        { "Admin", "POST", "/api/genre", """{"name":"{unique}"}""", HttpStatusCode.Created },

        // Movie (404 = pasó la autorización y llegó al servicio)
        { Anonymous, "GET", "/api/movie/active", null, HttpStatusCode.OK },
        { Anonymous, "DELETE", $"/api/movie/{NoSuchId}", null, HttpStatusCode.Unauthorized },
        { "Client", "DELETE", $"/api/movie/{NoSuchId}", null, HttpStatusCode.Forbidden },
        { "Admin", "DELETE", $"/api/movie/{NoSuchId}", null, HttpStatusCode.NotFound },

        // Screen
        { Anonymous, "GET", "/api/screen", null, HttpStatusCode.OK },
        { Anonymous, "POST", "/api/screen", """{"name":"{unique}","capacity":50}""", HttpStatusCode.Unauthorized },
        { "Client", "POST", "/api/screen", """{"name":"{unique}","capacity":50}""", HttpStatusCode.Forbidden },
        { "SuperAdmin", "POST", "/api/screen", """{"name":"{unique}","capacity":50}""", HttpStatusCode.Created },

        // Showtime
        { Anonymous, "GET", "/api/showtime", null, HttpStatusCode.OK },
        { Anonymous, "DELETE", $"/api/showtime/{NoSuchId}", null, HttpStatusCode.Unauthorized },
        { "Client", "DELETE", $"/api/showtime/{NoSuchId}", null, HttpStatusCode.Forbidden },
        { "Admin", "DELETE", $"/api/showtime/{NoSuchId}", null, HttpStatusCode.NotFound },

        // Ticket: el listado completo es del staff
        { Anonymous, "GET", "/api/ticket", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/ticket", null, HttpStatusCode.Forbidden },
        { "Admin", "GET", "/api/ticket", null, HttpStatusCode.OK },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Endpoint_RespondsAccordingToTheRole(string role, string method, string url, string? body, HttpStatusCode expected)
    {
        using var client = api.CreateClient(role == Anonymous ? null : api.TokenFor(role));
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (body is not null)
        {
            request.Content = new StringContent(body.Replace("{unique}", $"Test {Guid.NewGuid():N}"[..20], StringComparison.Ordinal), Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(Anonymous, HttpStatusCode.Unauthorized, "No autenticado")]
    [InlineData("Client", HttpStatusCode.Forbidden, "Acceso denegado")]
    public async Task UnauthorizedAndForbidden_AreProblemDetailsInSpanish(string role, HttpStatusCode status, string title)
    {
        using var client = api.CreateClient(role == Anonymous ? null : api.TokenFor(role));

        using var response = await client.GetAsync("/api/user");

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(title, problem.RootElement.GetProperty("title").GetString());
        Assert.False(string.IsNullOrEmpty(problem.RootElement.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task Login_WithAWrongPassword_Returns401ProblemDetails()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = ApiFixture.SuperAdminEmail, password = "incorrecta" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Email o contraseña incorrectos.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Responses_NeverIncludeThePasswordOrItsHash()
    {
        using var asAnonymous = api.CreateClient();
        using var asSuperAdmin = api.CreateClient(api.SuperAdminToken);

        var login = await asAnonymous.PostAsJsonAsync("/api/auth/login", new { email = ApiFixture.SuperAdminEmail, password = ApiFixture.SuperAdminPassword });
        var users = await asSuperAdmin.GetAsync("/api/user");
        var me = await asSuperAdmin.GetAsync("/api/auth/me");

        foreach (var response in new[] { login, users, me })
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("password", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("Admin", "Admin")]
    [InlineData("Admin", "SuperAdmin")]
    [InlineData("SuperAdmin", "SuperAdmin")]
    public async Task CreateUser_WithARoleTheActorCannotAssign_Returns403(string actor, string role)
    {
        using var client = api.CreateClient(api.TokenFor(actor));

        using var response = await client.PostAsJsonAsync("/api/user",
            new { name = "Nuevo", email = ApiFixture.UniqueEmail("nuevo"), password = ApiFixture.DefaultPassword, role });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAnExistingEmail_Returns409()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register",
            new { name = "Copia", email = ApiFixture.SuperAdminEmail.ToUpperInvariant(), password = ApiFixture.DefaultPassword });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Dar de baja a un cliente corta su acceso en el momento, aunque su token siga vigente.
    [Fact]
    public async Task Token_OfADeletedUser_IsRejected()
    {
        var (token, id) = await api.RegisterClientAsync();
        using var asClient = api.CreateClient(token);
        using var asAdmin = api.CreateClient(api.AdminToken);
        Assert.Equal(HttpStatusCode.OK, (await asClient.GetAsync("/api/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await asAdmin.DeleteAsync($"/api/user/{id}")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await asClient.GetAsync("/api/auth/me")).StatusCode);
    }

    // Un Admin pasado a Client no conserva los permisos de staff hasta que venza su token.
    [Fact]
    public async Task Token_IsRejectedAfterARoleChange()
    {
        var email = ApiFixture.UniqueEmail("degradado");
        using var asSuperAdmin = api.CreateClient(api.SuperAdminToken);
        var created = await asSuperAdmin.PostAsJsonAsync("/api/user", new { name = "Degradado", email, password = ApiFixture.DefaultPassword, role = "Admin" });
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetGuid();
        using var asFormerAdmin = api.CreateClient(await api.LoginAsync(email, ApiFixture.DefaultPassword));
        Assert.Equal(HttpStatusCode.OK, (await asFormerAdmin.GetAsync("/api/user")).StatusCode);

        var demoted = await asSuperAdmin.PutAsJsonAsync($"/api/user/{id}", new { name = "Degradado", email, role = "Client" });
        Assert.Equal(HttpStatusCode.NoContent, demoted.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await asFormerAdmin.GetAsync("/api/user")).StatusCode);
        using var asClient = api.CreateClient(await api.LoginAsync(email, ApiFixture.DefaultPassword));
        Assert.Equal(HttpStatusCode.Forbidden, (await asClient.GetAsync("/api/user")).StatusCode);
    }

    // Las contraseñas de antes de la autenticación estaban en texto plano: al arrancar se reemplazan por
    // su hash y se puede seguir entrando con la misma contraseña.
    [Fact]
    public async Task DataInitializer_HashesLegacyPlainTextPasswords()
    {
        var email = ApiFixture.UniqueEmail("legacy");
        await using (var context = api.Database.CreateContext())
        {
            context.Users.Add(new User { Id = Guid.NewGuid(), Name = "Legacy", Email = email, PasswordHash = "vieja-clave", Role = Roles.Client });
            await context.SaveChangesAsync();
        }

        await DataInitializer.InitializeAsync(api.Services, seedDemoData: false);

        await using (var context = api.Database.CreateContext())
        {
            var stored = await context.Users.SingleAsync(u => u.Email == email);
            Assert.NotEqual("vieja-clave", stored.PasswordHash);
            Assert.True(api.Services.GetRequiredService<IPasswordHasher>().Verify(stored.PasswordHash, "vieja-clave"));
        }

        Assert.False(string.IsNullOrEmpty(await api.LoginAsync(email, "vieja-clave")));
    }
}
