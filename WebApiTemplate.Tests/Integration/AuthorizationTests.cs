using System.Net;
using System.Net.Http.Json;
using WebApiTemplate.Tests.Integration.Infrastructure;

namespace WebApiTemplate.Tests.Integration;

public class AuthorizationTests
{
    // PUT /api/v1/logging/level — AdminOnly
    [Test]
    public async Task AdminOnly_WithAdmin_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("admin", "Admin");

        var response = await client.PutAsJsonAsync("/api/v1/logging/level", new
        {
            category = "Microsoft",
            level = "Warning"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AdminOnly_WithOperator_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("operator", "Operator");

        var response = await client.PutAsJsonAsync("/api/v1/logging/level", new
        {
            category = "Microsoft",
            level = "Warning"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AdminOnly_WithAuditor_Returns403()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("auditor", "Auditor");

        var response = await client.PutAsJsonAsync("/api/v1/logging/level", new
        {
            category = "Microsoft",
            level = "Warning"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    // GET /api/v1/logging/categories — AuditViewer
    [Test]
    public async Task AuditViewer_WithAuditor_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("auditor", "Auditor");

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AuditViewer_WithOperator_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("operator", "Operator");

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AuditViewer_WithAdmin_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        var client = factory.CreateAuthenticatedClient("admin", "Admin");

        var response = await client.GetAsync("/api/v1/logging/categories");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
