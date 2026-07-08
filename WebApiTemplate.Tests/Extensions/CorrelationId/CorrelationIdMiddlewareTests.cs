using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using WebApiTemplate.Extensions.CorrelationId;

namespace WebApiTemplate.Tests.Extensions.CorrelationId;

public class CorrelationIdMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_WhenHeaderMissing_GeneratesCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var id = context.Items["CorrelationId"] as string;
        await Assert.That(id).IsNotNull();
        await Assert.That(Guid.TryParse(id, out _)).IsTrue();
    }

    [Test]
    public async Task InvokeAsync_WhenHeaderPresent_UsesIncomingCorrelationId()
    {
        var expected = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = expected;
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var id = context.Items["CorrelationId"] as string;
        await Assert.That(id).IsEqualTo(expected);
    }

    [Test]
    public async Task InvokeAsync_SetsCorrelationIdInItems()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        await Assert.That(context.Items.ContainsKey("CorrelationId")).IsTrue();
        await Assert.That(context.Items["CorrelationId"]).IsNotNull();
    }

    [Test]
    public async Task InvokeAsync_SetsActivityTag()
    {
        using var activity = new Activity("Test").Start();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var tag = Activity.Current?.GetTagItem("correlation.id") as string;
        await Assert.That(tag).IsNotNull();
        var itemId = context.Items["CorrelationId"] as string;
        await Assert.That(tag).IsEqualTo(itemId);
    }
}
