using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApiTemplate.HealthChecks;

namespace WebApiTemplate.Extensions.HealthChecks;

// ============================================================================
// EXTENSION-МЕТОДЫ ДЛЯ HEALTH CHECKS (DI + Pipeline)
// ============================================================================
public static class HealthCheckExtensions
{
    // ------------------------------------------------------------------------
    // AddCustomHealthChecks — регистрация health checks в DI.
    // Liveness: delegate, без зависимостей, игнорирует shutdown (всегда Healthy).
    // Readiness: класс с кэшем + IDatabaseHealthChecker.
    // ------------------------------------------------------------------------
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddScoped<IDatabaseHealthChecker, DatabaseHealthChecker>();

        services.AddHealthChecks()
            .AddCheck("live", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<ReadinessHealthCheck>("ready", tags: ["ready"]);

        return services;
    }

    // ------------------------------------------------------------------------
    // UseCustomHealthChecks — привязка health-эндпоинтов к порту из конфигурации.
    // Порт: HealthPort в appsettings.json (по умолчанию 8081).
    // Rate limiting: 30 запросов за 10 секунд.
    // ------------------------------------------------------------------------
    public static WebApplication UseCustomHealthChecks(this WebApplication app)
    {
        var healthPort = app.Configuration.GetValue<int>("HealthPort", 8081);

        app.MapWhen(ctx => ctx.Connection.LocalPort == healthPort, app =>
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("live"),
                    ResponseWriter = MinimalResponseWriter.WriteMinimal
                }).RequireRateLimiting("health");

                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = r => r.Tags.Contains("ready"),
                    ResponseWriter = MinimalResponseWriter.WriteMinimal
                }).RequireRateLimiting("health");

                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = MinimalResponseWriter.WriteMinimal
                }).RequireRateLimiting("health");
            });
        });

        return app;
    }
}
