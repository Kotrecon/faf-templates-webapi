namespace WebApiTemplate.Extensions.Cors;

// ============================================================================
// CORS POLICY
// Текущая политика: AllowAll (для разработки).
// TODO: см. architecture/TODO.md — ограничить origins для production.
// ============================================================================
public static class CorsExtensions
{
    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}
