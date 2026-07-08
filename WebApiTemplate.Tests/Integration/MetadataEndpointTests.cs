using System.Net;
using System.Text.Json;
using WebApiTemplate.Tests.Integration.Infrastructure;

namespace WebApiTemplate.Tests.Integration;

public class MetadataEndpointTests
{
    [Test]
    public async Task GetMetadata_Returns200_WithCorrectData()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/metadata");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        await Assert.That(json.GetProperty("title").GetString()).IsEqualTo("Test API");
        await Assert.That(json.GetProperty("version").GetString()).IsEqualTo("1.0.0");
        await Assert.That(json.GetProperty("description").GetString()).IsEqualTo("Test description");
        await Assert.That(json.GetProperty("developer").GetProperty("name").GetString()).IsEqualTo("Test Dev");
    }

    [Test]
    public async Task GetMetadata_WithoutAuth_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        // Без Authorization header

        var response = await client.GetAsync("/api/metadata");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
