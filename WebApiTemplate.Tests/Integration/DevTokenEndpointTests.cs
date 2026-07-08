using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WebApiTemplate.Tests.Integration.Infrastructure;

namespace WebApiTemplate.Tests.Integration;

public class DevTokenEndpointTests
{
    [Test]
    public async Task DevToken_Returns200_WithValidJwt()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/dev/token", new
        {
            username = "admin",
            roles = new[] { "Admin" }
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        await Assert.That(json.GetProperty("token").GetString()).IsNotNull();
        await Assert.That(json.GetProperty("expires").GetInt32()).IsEqualTo(8);
    }

    [Test]
    public async Task DevToken_GeneratedToken_PassesAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        // Получаем токен
        var tokenResponse = await client.PostAsJsonAsync("/dev/token", new
        {
            username = "admin",
            roles = new[] { "Admin" }
        });
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<JsonElement>(tokenBody).GetProperty("token").GetString();

        // Используем токен на защищённом эндпоинте
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var protectedResponse = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(protectedResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DevToken_WithMultipleRoles_GeneratesValidToken()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/dev/token", new
        {
            username = "user",
            roles = new[] { "Admin", "Operator", "Auditor" }
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DevToken_NotAvailable_InProduction()
    {
        var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production"
        });
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/dev/token", new
        {
            username = "admin",
            roles = new[] { "Admin" }
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
