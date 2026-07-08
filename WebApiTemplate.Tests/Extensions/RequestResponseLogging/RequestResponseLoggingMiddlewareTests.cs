using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebApiTemplate.Extensions.RequestResponseLogging;

namespace WebApiTemplate.Tests.Extensions.RequestResponseLogging;

public class RequestResponseLoggingMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_LogsSuccessfulRequest()
    {
        var logger = new Mock<ILogger<RequestResponseLoggingMiddleware>>();
        var middleware = new RequestResponseLoggingMiddleware(
            ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; },
            logger.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";

        await middleware.InvokeAsync(context);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("GET") &&
                    v.ToString()!.Contains("/api/test") &&
                    v.ToString()!.Contains("200") &&
                    v.ToString()!.Contains("ms")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task InvokeAsync_LogsErrorStatusAsWarning()
    {
        var logger = new Mock<ILogger<RequestResponseLoggingMiddleware>>();
        var middleware = new RequestResponseLoggingMiddleware(
            ctx => { ctx.Response.StatusCode = 400; return Task.CompletedTask; },
            logger.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/test";

        await middleware.InvokeAsync(context);

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
