namespace WebApiTemplate.Extensions.ExceptionHandler;

// ============================================================================
// EXCEPTION HANDLER EXTENSIONS
// ============================================================================
public static class ExceptionHandlerExtensions
{
    public static IServiceCollection AddCustomExceptionHandler(this IServiceCollection services)
    {
        return services;
    }

    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlerMiddleware>();
    }
}
