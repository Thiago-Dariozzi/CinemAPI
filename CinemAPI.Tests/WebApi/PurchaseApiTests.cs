using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CinemAPI.Tests.WebApi;

// La compra de punta a punta: model binding, políticas, reglas del servicio, índice único y ProblemDetails.
[Collection(ApiTestGroup.Name)]
public sealed class PurchaseApiTests(ApiFixture api)
{
    private static readonly DateTime Tomorrow = DateTime.Now.Date.AddDays(1).AddHours(20);

    [Fact]
    public async Task Purchase_ShowsUpInTheSeatMapInMineAndInSales_AndCancellingFreesTheSeats()
    {
        var showtimeId = await api.CreateShowtimeAsync(Tomorrow, price: 12.5m);
        using var asClient = api.CreateClient(api.ClientToken);
        using var asAdmin = api.CreateClient(api.AdminToken);

        var created = await asClient.PostAsJsonAsync("/api/purchase", Purchase(showtimeId, (2, 3), (2, 4)));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        using var purchase = await ReadJsonAsync(created);
        var purchaseId = purchase.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(25m, purchase.RootElement.GetProperty("total").GetDecimal());
        Assert.Equal("Confirmed", purchase.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, purchase.RootElement.GetProperty("tickets").GetArrayLength());

        Assert.Equal([(2, 3), (2, 4)], await TakenSeatsAsync(showtimeId));
        Assert.Contains(purchaseId, await IdsAsync(asClient, "/api/purchase/mine"));
        Assert.Equal([purchaseId], await IdsAsync(asAdmin, $"/api/purchase?showtimeId={showtimeId}"));

        var cancelled = await asClient.PostAsync($"/api/purchase/mine/{purchaseId}/cancel", content: null);

        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        using var cancelledBody = await ReadJsonAsync(cancelled);
        Assert.Equal("Cancelled", cancelledBody.RootElement.GetProperty("status").GetString());
        Assert.Empty(await TakenSeatsAsync(showtimeId));
    }

    [Fact]
    public async Task Purchase_OfATakenSeat_Returns409ProblemDetailsNamingTheSeat()
    {
        var showtimeId = await api.CreateShowtimeAsync(Tomorrow);
        using var first = api.CreateClient(api.ClientToken);
        using var second = api.CreateClient((await api.RegisterClientAsync()).Token);
        (await first.PostAsJsonAsync("/api/purchase", Purchase(showtimeId, (1, 5)))).EnsureSuccessStatusCode();

        var response = await second.PostAsJsonAsync("/api/purchase", Purchase(showtimeId, (1, 4), (1, 5)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = await ReadJsonAsync(response);
        Assert.Contains("A5", problem.RootElement.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    // La carrera: varios clientes piden la misma butaca a la vez. Pasen o no juntos la validación del
    // servicio, el índice único deja una sola venta y el resto recibe 409 (nunca 500).
    [Fact]
    public async Task ConcurrentPurchasesOfTheSameSeat_OnlyOneSucceeds()
    {
        const int Buyers = 8;
        var showtimeId = await api.CreateShowtimeAsync(Tomorrow);
        var clients = new List<HttpClient>();
        for (var i = 0; i < Buyers; i++)
        {
            clients.Add(api.CreateClient((await api.RegisterClientAsync()).Token));
        }

        var responses = await Task.WhenAll(clients.Select(c => c.PostAsJsonAsync("/api/purchase", Purchase(showtimeId, (3, 3)))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal([(3, 3)], await TakenSeatsAsync(showtimeId));
        clients.ForEach(c => c.Dispose());
    }

    [Fact]
    public async Task Cancel_APurchaseOfAnotherClient_Returns403_ButStaffCancelsItWithItsOwnRoute()
    {
        var showtimeId = await api.CreateShowtimeAsync(Tomorrow);
        using var owner = api.CreateClient(api.ClientToken);
        using var stranger = api.CreateClient((await api.RegisterClientAsync()).Token);
        var created = await owner.PostAsJsonAsync("/api/purchase", Purchase(showtimeId, (1, 1)));
        using var body = await ReadJsonAsync(created);
        var purchaseId = body.RootElement.GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync($"/api/purchase/mine/{purchaseId}/cancel", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PostAsync($"/api/purchase/{purchaseId}/cancel", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/purchase/{purchaseId}")).StatusCode);

        using var admin = api.CreateClient(api.TokenFor("Admin"));
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsync($"/api/purchase/mine/{purchaseId}/cancel", content: null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/purchase/{purchaseId}/cancel", content: null)).StatusCode);
    }

    [Fact]
    public async Task Purchase_ForAShowtimeThatAlreadyStarted_Returns400()
    {
        var showtimeId = await api.CreateShowtimeAsync(DateTime.Now.AddMinutes(-5));
        using var asClient = api.CreateClient(api.ClientToken);

        var response = await asClient.PostAsJsonAsync("/api/purchase", Purchase(showtimeId, (1, 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Purchase_WithoutSeats_Returns400ValidationProblem()
    {
        using var asClient = api.CreateClient(api.ClientToken);

        var response = await asClient.PostAsJsonAsync("/api/purchase", new { showtimeId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = await ReadJsonAsync(response);
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
    }

    private static object Purchase(Guid showtimeId, params (int Row, int Number)[] seats) =>
        new { showtimeId, seats = seats.Select(s => new { row = s.Row, number = s.Number }) };

    private async Task<List<(int, int)>> TakenSeatsAsync(Guid showtimeId)
    {
        using var anonymous = api.CreateClient();
        using var map = await ReadJsonAsync(await anonymous.GetAsync($"/api/showtime/{showtimeId}/seats"));
        return map.RootElement.GetProperty("takenSeats").EnumerateArray()
            .Select(s => (s.GetProperty("row").GetInt32(), s.GetProperty("number").GetInt32()))
            .ToList();
    }

    private static async Task<List<Guid>> IdsAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        using var list = await ReadJsonAsync(response);
        return list.RootElement.EnumerateArray().Select(p => p.GetProperty("id").GetGuid()).ToList();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}
