using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApiTemplate.HealthChecks;

// ============================================================================
// MINIMAL RESPONSE WRITER ДЛЯ HEALTH CHECKS
// Возвращает только HTTP-код + status, без деталей (сигнатурализация).
// Использование: ResponseWriter = MinimalResponseWriter.WriteMinimal
// ============================================================================
public static class MinimalResponseWriter
{
    public static Task WriteMinimal(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString()
        });

        return context.Response.WriteAsync(response);
    }
}

