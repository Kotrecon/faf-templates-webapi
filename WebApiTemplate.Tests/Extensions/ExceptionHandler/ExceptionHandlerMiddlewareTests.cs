using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebApiTemplate.Extensions.ExceptionHandler;

namespace WebApiTemplate.Tests.Extensions.ExceptionHandler;

public class ExceptionHandlerMiddlewareTests
{
    private static readonly Dictionary<Type, Func<Exception>> ExceptionFactories = new()
    {
        [typeof(ArgumentException)] = () => new ArgumentException("test"),
        [typeof(KeyNotFoundException)] = () => new KeyNotFoundException("test"),
        [typeof(UnauthorizedAccessException)] = () => new UnauthorizedAccessException("test"),
        [typeof(TimeoutException)] = () => new TimeoutException("test"),
        [typeof(InvalidOperationException)] = () => new InvalidOperationException("test")
    };

    [Test]
    [Arguments(typeof(ArgumentException), 400)]
    [Arguments(typeof(KeyNotFoundException), 404)]
    [Arguments(typeof(UnauthorizedAccessException), 403)]
    [Arguments(typeof(TimeoutException), 504)]
    [Arguments(typeof(InvalidOperationException), 500)]
    public async Task InvokeAsync_MapsExceptionToCorrectStatusCode(Type exceptionType, int expectedStatus)
    {
        var middleware = new ExceptionHandlerMiddleware(
            _ => throw ExceptionFactories[exceptionType](),
            Mock.Of<ILogger<ExceptionHandlerMiddleware>>());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        await Assert.That(context.Response.StatusCode).IsEqualTo(expectedStatus);
    }

    [Test]
    public async Task InvokeAsync_ReturnsCorrectJsonFormat()
    {
        var middleware = new ExceptionHandlerMiddleware(
            _ => throw new ArgumentException("bad param"),
            Mock.Of<ILogger<ExceptionHandlerMiddleware>>());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var json = JsonDocument.Parse(body);
        var error = json.RootElement.GetProperty("error");

        await Assert.That(error.GetProperty("code").GetInt32()).IsEqualTo(400);
        await Assert.That(error.GetProperty("message").GetString()).IsEqualTo("Invalid request.");
    }

    [Test]
    public async Task InvokeAsync_DoesNotLeakInternalDetails()
    {
        var middleware = new ExceptionHandlerMiddleware(
            _ => throw new Exception("Internal path: C:\\secret"),
            Mock.Of<ILogger<ExceptionHandlerMiddleware>>());
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        await Assert.That(body).DoesNotContain("StackTrace");
        await Assert.That(body).DoesNotContain("InnerException");
        await Assert.That(body).DoesNotContain("secret");
        await Assert.That(body).DoesNotContain("C:\\");
    }
}
