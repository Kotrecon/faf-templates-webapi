using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace WebApiTemplate.Extensions.RateLimiting;

// ============================================================================
// EXTENSION-МЕТОД ДЛЯ RATE LIMITING
// Ограничивает количество запросов к health-эндпоинтам.
// ============================================================================
public static class RateLimitingExtensions
{
    // ------------------------------------------------------------------------
    // AddCustomRateLimiting — регистрация rate limiter в DI.
    // Лимит: 10 запросов за 10 секунд на health-ветку.
    // ------------------------------------------------------------------------
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("health", opt =>
            {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromSeconds(10);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        return services;
    }
}
