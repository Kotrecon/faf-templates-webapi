using System.Net;
using WebApiTemplate.Tests.Integration.Infrastructure;

namespace WebApiTemplate.Tests.Integration;

public class CorrelationIdE2ETests
{
    [Test]
    public async Task Request_WithoutCorrelationId_ReturnsGeneratedId()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/metadata");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("X-Correlation-Id")).IsTrue();

        var correlationId = response.Headers.GetValues("X-Correlation-Id").First();
        await Assert.That(Guid.TryParse(correlationId, out _)).IsTrue();
    }

    [Test]
    public async Task Request_WithCorrelationId_ReturnsSameId()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        var incomingId = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", incomingId);

        var response = await client.GetAsync("/api/metadata");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Headers.Contains("X-Correlation-Id")).IsTrue();

        var returnedId = response.Headers.GetValues("X-Correlation-Id").First();
        await Assert.That(returnedId).IsEqualTo(incomingId);
    }
}
