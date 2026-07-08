namespace WebApiTemplate.Extensions.RequestResponseLogging;

// ============================================================================
// REQUEST/RESPONSE LOGGING EXTENSIONS
// ============================================================================
public static class RequestResponseLoggingExtensions
{
    public static IServiceCollection AddCustomRequestResponseLogging(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseCustomRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
