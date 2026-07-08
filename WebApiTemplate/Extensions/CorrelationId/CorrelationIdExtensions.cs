namespace WebApiTemplate.Extensions.CorrelationId;

// ============================================================================
// CORRELATION ID EXTENSIONS
// ============================================================================
public static class CorrelationIdExtensions
{
    public static IServiceCollection AddCustomCorrelationId(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseCustomCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
