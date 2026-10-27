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

    // Función inexistente: el cliente pasa la autorización y el servicio responde 400 (referencia inválida).
    private static readonly string PurchaseBody = $$"""{"showtimeId":"{{NoSuchId}}","seats":[{"row":1,"number":1}]}""";

    private const string FeeBody = """{"name":"{unique}","percentage":5,"fixedAmount":0,"isCurrent":false}""";

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
        { "Admin", "GET", "/api/user?role=Client&search=demo", null, HttpStatusCode.OK },
        { Anonymous, "POST", $"/api/user/{NoSuchId}/reactivate", null, HttpStatusCode.Unauthorized },
        { "Client", "POST", $"/api/user/{NoSuchId}/reactivate", null, HttpStatusCode.Forbidden },
        { "Admin", "POST", $"/api/user/{NoSuchId}/reactivate", null, HttpStatusCode.NotFound },

        // Cartelera: pública, con filtros
        { Anonymous, "GET", $"/api/movie/active?genreId={NoSuchId}&date=2026-11-02", null, HttpStatusCode.OK },
        { Anonymous, "GET", "/api/movie/active?date=no-es-fecha", null, HttpStatusCode.BadRequest },
        { Anonymous, "GET", "/api/movie/active?genreId=abc", null, HttpStatusCode.BadRequest },
        { Anonymous, "GET", $"/api/movie/{NoSuchId}/showtimes", null, HttpStatusCode.NotFound },

        // Reports: solo staff; las fechas son obligatorias y el rango tiene que ser válido
        { Anonymous, "GET", "/api/reports/sales?from=2026-10-01&to=2026-10-31", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/reports/sales?from=2026-10-01&to=2026-10-31", null, HttpStatusCode.Forbidden },
        { "Admin", "GET", "/api/reports/sales?from=2026-10-01&to=2026-10-31", null, HttpStatusCode.OK },
        { "SuperAdmin", "GET", "/api/reports/sales?from=2026-10-01&to=2026-10-31", null, HttpStatusCode.OK },
        { "Admin", "GET", "/api/reports/sales?from=2026-10-01", null, HttpStatusCode.BadRequest },
        { "Admin", "GET", "/api/reports/sales?from=2026-10-31&to=2026-10-01", null, HttpStatusCode.BadRequest },
        { "Admin", "GET", "/api/reports/sales?from=2025-01-01&to=2026-12-31", null, HttpStatusCode.BadRequest },
        { Anonymous, "GET", "/api/reports/occupancy?date=2026-10-27", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/reports/occupancy?date=2026-10-27", null, HttpStatusCode.Forbidden },
        { "Admin", "GET", "/api/reports/occupancy?date=2026-10-27", null, HttpStatusCode.OK },
        { "SuperAdmin", "GET", "/api/reports/occupancy?date=2026-10-27", null, HttpStatusCode.OK },
        { "Admin", "GET", "/api/reports/occupancy", null, HttpStatusCode.BadRequest },
        { "Admin", "GET", "/api/reports/occupancy?date=ayer", null, HttpStatusCode.BadRequest },
        { "Admin", "GET", "/api/reports/occupancy?date=9999-12-31", null, HttpStatusCode.BadRequest },

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
        { Anonymous, "POST", "/api/screen", """{"name":"{unique}","rows":5,"seatsPerRow":10}""", HttpStatusCode.Unauthorized },
        { "Client", "POST", "/api/screen", """{"name":"{unique}","rows":5,"seatsPerRow":10}""", HttpStatusCode.Forbidden },
        { "SuperAdmin", "POST", "/api/screen", """{"name":"{unique}","rows":5,"seatsPerRow":10}""", HttpStatusCode.Created },

        // Showtime
        { Anonymous, "GET", "/api/showtime", null, HttpStatusCode.OK },
        { Anonymous, "DELETE", $"/api/showtime/{NoSuchId}", null, HttpStatusCode.Unauthorized },
        { "Client", "DELETE", $"/api/showtime/{NoSuchId}", null, HttpStatusCode.Forbidden },
        { "Admin", "DELETE", $"/api/showtime/{NoSuchId}", null, HttpStatusCode.NotFound },

        // Mapa de butacas y próximas funciones: públicos
        { Anonymous, "GET", "/api/showtime/upcoming", null, HttpStatusCode.OK },
        { Anonymous, "GET", $"/api/showtime/{NoSuchId}/seats", null, HttpStatusCode.NotFound },

        // Fee: el cargo vigente lo ve cualquier usuario logueado (204: no hay ninguno en la base de prueba)
        { Anonymous, "GET", "/api/fee/current", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/fee/current", null, HttpStatusCode.NoContent },

        // Fee: el ABM es solo del SuperAdmin (los altas de prueba no quedan vigentes)
        { Anonymous, "GET", "/api/fee", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/fee", null, HttpStatusCode.Forbidden },
        { "Admin", "GET", "/api/fee", null, HttpStatusCode.Forbidden },
        { "SuperAdmin", "GET", "/api/fee", null, HttpStatusCode.OK },
        { Anonymous, "GET", $"/api/fee/{NoSuchId}", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", $"/api/fee/{NoSuchId}", null, HttpStatusCode.Forbidden },
        { "Admin", "GET", $"/api/fee/{NoSuchId}", null, HttpStatusCode.Forbidden },
        { "SuperAdmin", "GET", $"/api/fee/{NoSuchId}", null, HttpStatusCode.NotFound },
        { Anonymous, "POST", "/api/fee", FeeBody, HttpStatusCode.Unauthorized },
        { "Client", "POST", "/api/fee", FeeBody, HttpStatusCode.Forbidden },
        { "Admin", "POST", "/api/fee", FeeBody, HttpStatusCode.Forbidden },
        { "SuperAdmin", "POST", "/api/fee", FeeBody, HttpStatusCode.Created },
        { "SuperAdmin", "POST", "/api/fee", """{"name":"Caro","percentage":101,"fixedAmount":0,"isCurrent":false}""", HttpStatusCode.BadRequest },
        { "SuperAdmin", "POST", "/api/fee", """{"name":"Negativo","percentage":5,"fixedAmount":-1,"isCurrent":false}""", HttpStatusCode.BadRequest },
        { "SuperAdmin", "POST", "/api/fee", """{"name":"Decimales","percentage":12.345,"fixedAmount":0,"isCurrent":false}""", HttpStatusCode.BadRequest },
        { "SuperAdmin", "POST", "/api/fee", """{"name":"Sin vigente","percentage":5,"fixedAmount":0}""", HttpStatusCode.BadRequest },
        { Anonymous, "PUT", $"/api/fee/{NoSuchId}", FeeBody, HttpStatusCode.Unauthorized },
        { "Client", "PUT", $"/api/fee/{NoSuchId}", FeeBody, HttpStatusCode.Forbidden },
        { "Admin", "PUT", $"/api/fee/{NoSuchId}", FeeBody, HttpStatusCode.Forbidden },
        { "SuperAdmin", "PUT", $"/api/fee/{NoSuchId}", FeeBody, HttpStatusCode.NotFound },
        { Anonymous, "DELETE", $"/api/fee/{NoSuchId}", null, HttpStatusCode.Unauthorized },
        { "Client", "DELETE", $"/api/fee/{NoSuchId}", null, HttpStatusCode.Forbidden },
        { "Admin", "DELETE", $"/api/fee/{NoSuchId}", null, HttpStatusCode.Forbidden },
        { "SuperAdmin", "DELETE", $"/api/fee/{NoSuchId}", null, HttpStatusCode.NotFound },

        // Purchase: compra, "mis compras" y cancelar las propias solo clientes; el listado de ventas y
        // cancelar cualquier compra solo staff
        { Anonymous, "POST", "/api/purchase", PurchaseBody, HttpStatusCode.Unauthorized },
        { "Admin", "POST", "/api/purchase", PurchaseBody, HttpStatusCode.Forbidden },
        { "Client", "POST", "/api/purchase", PurchaseBody, HttpStatusCode.BadRequest },
        { Anonymous, "GET", "/api/purchase/mine", null, HttpStatusCode.Unauthorized },
        { "Admin", "GET", "/api/purchase/mine", null, HttpStatusCode.Forbidden },
        { "Client", "GET", "/api/purchase/mine", null, HttpStatusCode.OK },
        { Anonymous, "GET", "/api/purchase", null, HttpStatusCode.Unauthorized },
        { "Client", "GET", "/api/purchase", null, HttpStatusCode.Forbidden },
        { "Admin", "GET", "/api/purchase", null, HttpStatusCode.OK },
        { "SuperAdmin", "GET", "/api/purchase", null, HttpStatusCode.OK },
        { Anonymous, "POST", $"/api/purchase/mine/{NoSuchId}/cancel", null, HttpStatusCode.Unauthorized },
        { "Admin", "POST", $"/api/purchase/mine/{NoSuchId}/cancel", null, HttpStatusCode.Forbidden },
        { "Client", "POST", $"/api/purchase/mine/{NoSuchId}/cancel", null, HttpStatusCode.NotFound },
        { Anonymous, "POST", $"/api/purchase/{NoSuchId}/cancel", null, HttpStatusCode.Unauthorized },
        { "Client", "POST", $"/api/purchase/{NoSuchId}/cancel", null, HttpStatusCode.Forbidden },
        { "Admin", "POST", $"/api/purchase/{NoSuchId}/cancel", null, HttpStatusCode.NotFound },
        { "SuperAdmin", "POST", $"/api/purchase/{NoSuchId}/cancel", null, HttpStatusCode.NotFound },
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

    // Alta de un Admin por el SuperAdmin, baja (no puede entrar) y reactivación (vuelve a entrar). El Admin
    // no puede reactivarse ni darse de baja entre pares, y el SuperAdmin no puede darse de baja a sí mismo.
    [Fact]
    public async Task Admin_CreatedDeletedAndReactivatedBySuperAdmin()
    {
        var email = ApiFixture.UniqueEmail("admin");
        using var asSuperAdmin = api.CreateClient(api.SuperAdminToken);
        using var asOtherAdmin = api.CreateClient(api.AdminToken);
        var created = await asSuperAdmin.PostAsJsonAsync("/api/user", new { name = "Nuevo Admin", email, password = ApiFixture.DefaultPassword, role = "Admin" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetGuid();
        await api.LoginAsync(email, ApiFixture.DefaultPassword);

        Assert.Equal(HttpStatusCode.Forbidden, (await asOtherAdmin.DeleteAsync($"/api/user/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await asSuperAdmin.DeleteAsync($"/api/user/{id}")).StatusCode);
        using var deletedLogin = await asOtherAdmin.PostAsJsonAsync("/api/auth/login", new { email, password = ApiFixture.DefaultPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, deletedLogin.StatusCode);
        using var listed = JsonDocument.Parse(await asSuperAdmin.GetStringAsync($"/api/user?role=Admin&search={Uri.EscapeDataString(email)}"));
        Assert.False(Assert.Single(listed.RootElement.EnumerateArray()).GetProperty("isActive").GetBoolean());

        Assert.Equal(HttpStatusCode.Forbidden, (await asOtherAdmin.PostAsync($"/api/user/{id}/reactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await asSuperAdmin.PostAsync($"/api/user/{id}/reactivate", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await asSuperAdmin.PostAsync($"/api/user/{id}/reactivate", content: null)).StatusCode);
        await api.LoginAsync(email, ApiFixture.DefaultPassword);

        using var me = JsonDocument.Parse(await asSuperAdmin.GetStringAsync("/api/auth/me"));
        var superAdminId = me.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await asSuperAdmin.DeleteAsync($"/api/user/{superAdminId}")).StatusCode);
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
