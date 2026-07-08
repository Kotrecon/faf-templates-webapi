using Microsoft.Extensions.Logging;

namespace WebApiTemplate.HealthChecks;

// ============================================================================
// ЗАГЛУШКА ПРОВЕРКИ БД
// Временно: всегда успешно. Позже заменить на реальную проверку БД.
// ============================================================================
public class DatabaseHealthChecker : IDatabaseHealthChecker
{
    private readonly ILogger<DatabaseHealthChecker> _logger;

    public DatabaseHealthChecker(ILogger<DatabaseHealthChecker> logger)
    {
        _logger = logger;
    }

    public Task CheckAsync(CancellationToken ct)
    {
        _logger.LogDebug("Database health check (stub) — always healthy");
        return Task.CompletedTask;
    }
}