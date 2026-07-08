using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApiTemplate.HealthChecks;

namespace WebApiTemplate.Tests.HealthChecks;

public class MinimalResponseWriterTests
{
    [Test]
    public async Task WriteMinimal_WhenHealthy_ReturnsStatusHealthy()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.Zero);

        await MinimalResponseWriter.WriteMinimal(context, report);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        await Assert.That(json.GetProperty("status").GetString()).IsEqualTo("Healthy");
    }

    [Test]
    public async Task WriteMinimal_WhenUnhealthy_ReturnsStatusUnhealthy()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["db"] = new HealthReportEntry(
                HealthStatus.Unhealthy,
                "Database unavailable",
                TimeSpan.FromMilliseconds(100),
                new Exception("Connection failed"),
                null)
        };

        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(100));

        await MinimalResponseWriter.WriteMinimal(context, report);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        await Assert.That(json.GetProperty("status").GetString()).IsEqualTo("Unhealthy");
    }

    [Test]
    public async Task WriteMinimal_DoesNotExposeInternalDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new Exception("Internal database connection failed");
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["db"] = new HealthReportEntry(
                HealthStatus.Unhealthy,
                "db down",
                TimeSpan.Zero,
                exception,
                null)
        };

        var report = new HealthReport(entries, TimeSpan.Zero);

        await MinimalResponseWriter.WriteMinimal(context, report);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        await Assert.That(body).DoesNotContain("database");
        await Assert.That(body).DoesNotContain("connection");
        await Assert.That(body).DoesNotContain("stack");
    }

    [Test]
    public async Task WriteMinimal_WhenDegraded_ReturnsStatusDegraded()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["db"] = new HealthReportEntry(
                HealthStatus.Degraded,
                "Slow response",
                TimeSpan.FromMilliseconds(100),
                null,
                null)
        };

        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(100));

        await MinimalResponseWriter.WriteMinimal(context, report);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        await Assert.That(json.GetProperty("status").GetString()).IsEqualTo("Degraded");
    }

    [Test]
    public async Task WriteMinimal_BodyIsNotEmpty()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.Zero);

        await MinimalResponseWriter.WriteMinimal(context, report);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        await Assert.That(body).IsNotEmpty();
        await Assert.That(body.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task WriteAsync_SetsContentType_ApplicationJson()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(),
            TimeSpan.Zero);

        await MinimalResponseWriter.WriteMinimal(httpContext, report);

        await Assert.That(httpContext.Response.ContentType).IsEqualTo("application/json");
    }
}
