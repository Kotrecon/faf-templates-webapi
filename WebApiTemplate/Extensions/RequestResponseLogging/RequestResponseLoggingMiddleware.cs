using System.Diagnostics;

namespace WebApiTemplate.Extensions.RequestResponseLogging;

// ============================================================================
// REQUEST/RESPONSE LOGGING MIDDLEWARE
// Логирует method, path, status code, duration.
// Не логирует тела request/response (чувствительные данные).
// ============================================================================
public sealed class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var duration = stopwatch.ElapsedMilliseconds;

            var level = statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            _logger.Log(level, "HTTP {Method} {Path} → {StatusCode} ({Duration}ms)",
                method, path, statusCode, duration);
        }
    }
}
