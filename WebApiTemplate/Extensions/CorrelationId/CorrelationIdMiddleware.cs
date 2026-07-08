using System.Diagnostics;
using Serilog.Context;

namespace WebApiTemplate.Extensions.CorrelationId;

// ============================================================================
// CORRELATION ID MIDDLEWARE
// Генерирует или прокидывает X-Correlation-Id через весь pipeline.
// Используется Guid.CreateVersion7() (.NET 10) для time-ordered ID.
// ============================================================================
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.Items["CorrelationId"] = correlationId;

        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values)
            && !string.IsNullOrWhiteSpace(values.First()))
        {
            return values.First()!;
        }

        return Guid.CreateVersion7().ToString();
    }
}
