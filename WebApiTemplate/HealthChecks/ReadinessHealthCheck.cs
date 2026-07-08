using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApiTemplate.HealthChecks;

// ============================================================================
// READINESS HEALTH CHECK
// Порядок проверки:
// 1. IsCancellationRequested → true? → сразу Unhealthy
// 2. Кэш свежий (< 5 сек)? → вернуть из кэша
// 3. Проверка через IDatabaseHealthChecker
// 4. Сохранить в кэш
// 5. Вернуть результат
// ============================================================================
public sealed class ReadinessHealthCheck : IHealthCheck
{
    private volatile CachedResult? _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(5);

    private readonly CancellationToken _shutdownToken;
    private readonly ILogger<ReadinessHealthCheck> _logger;
    private readonly IDatabaseHealthChecker _dbChecker;

    public ReadinessHealthCheck(
        IHostApplicationLifetime lifetime,
        ILogger<ReadinessHealthCheck> logger,
        IDatabaseHealthChecker dbChecker)
    {
        _shutdownToken = lifetime.ApplicationStopping;
        _logger = logger;
        _dbChecker = dbChecker;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // --------------------------------------------------------------------
        // 1. Shutdown — сразу Unhealthy (только readiness, liveness игнорирует)
        // --------------------------------------------------------------------
        if (_shutdownToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Service is shutting down");
        }

        // --------------------------------------------------------------------
        // 2. Кэш — если свежий результат (< 5 сек), вернуть его
        // --------------------------------------------------------------------
        if (_cache is { IsValid: true } cached)
        {
            return cached.Result;
        }

        // --------------------------------------------------------------------
        // 3. Проверка через IDatabaseHealthChecker
        // --------------------------------------------------------------------
        try
        {
            await _dbChecker.CheckAsync(cancellationToken);

            var result = HealthCheckResult.Healthy();

            // ----------------------------------------------------------------
            // 4. Сохранить в кэш
            // ----------------------------------------------------------------
            _cache = new CachedResult(result, DateTime.UtcNow);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            var result = HealthCheckResult.Unhealthy("Database is unavailable", ex);
            _cache = new CachedResult(result, DateTime.UtcNow);
            return result;
        }
    }

    // ------------------------------------------------------------------------
    // Кэш-запись с TTL
    // ------------------------------------------------------------------------
    private sealed record CachedResult(HealthCheckResult Result, DateTime CreatedAt)
    {
        public bool IsValid => DateTime.UtcNow - CreatedAt < CacheDuration;
    }
}
