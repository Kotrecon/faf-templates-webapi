using System.Net.Mime;
using System.Text.Json;

namespace WebApiTemplate.Extensions.ExceptionHandler;

// ============================================================================
// GLOBAL EXCEPTION HANDLER MIDDLEWARE
// Перехватывает все необработанные исключения и возвращает единообразный
// JSON-ответ. Не раскрывает stack traces, IP, внутренние пути.
// ============================================================================
public sealed class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static (int StatusCode, string Message) MapException(Exception exception) => exception switch
    {
        ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request."),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found."),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied."),
        TimeoutException => (StatusCodes.Status504GatewayTimeout, "Request timed out."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var (statusCode, message) = MapException(exception);
        context.Response.StatusCode = statusCode;

        var response = JsonSerializer.Serialize(new
        {
            error = new
            {
                code = statusCode,
                message
            }
        });

        return context.Response.WriteAsync(response);
    }
}
