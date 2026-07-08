using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using WebApiTemplate.Configuration.Options;
using WebApiTemplate.Extensions;
using WebApiTemplate.Extensions.CorrelationId;
using WebApiTemplate.Extensions.Cors;
using WebApiTemplate.Extensions.ExceptionHandler;
using WebApiTemplate.Extensions.HealthChecks;
using WebApiTemplate.Extensions.RateLimiting;
using WebApiTemplate.Extensions.RequestResponseLogging;
using WebApiTemplate.Security;

// ============================================================================
// ТОЧКА ВХОДА. Fail-fast: если что-то пошло не так при старте — логируем и
// завершаемся с кодом 1, без исключений в консоль.
// ============================================================================
try
{
    var builder = WebApplication.CreateBuilder(args);

    // ------------------------------------------------------------------------
    // 1. ЛОГИРОВАНИЕ (Serilog) — ПЕРВЫМ
    // Настраивается до валидации, чтобы ILogger работал через Serilog.
    // ------------------------------------------------------------------------
    builder.AddCustomLogging();

    // ------------------------------------------------------------------------
    // 2. ВАЛИДАЦИЯ КОНФИГУРАЦИИ
    // Fail-fast: если обязательные секции отсутствуют — выходим с кодом 1.
    // Дублируем в Console — Serilog может не успеть инициализироваться.
    // ------------------------------------------------------------------------
    if (!builder.AddAppSettings())
    {
        Console.WriteLine("[FATAL] AppSettings section is required");
        Log.Fatal("AppSettings section is required");
        Environment.Exit(1);
    }

    if (!builder.AddJwt())
    {
        Console.WriteLine("[FATAL] Jwt section is required");
        Log.Fatal("Jwt section is required");
        Environment.Exit(1);
    }

    if (!builder.AddApiMetadata())
    {
        Console.WriteLine("[FATAL] ApiMetadata section is required");
        Log.Fatal("ApiMetadata section is required");
        Environment.Exit(1);
    }

    // ------------------------------------------------------------------------
    // 3. ТЕЛЕМЕТРИЯ (OpenTelemetry: logs + traces + metrics → OTLP)
    // Pipeline строится один раз при старте. Endpoint/Protocol/Sinks
    // поменять на лету нельзя — только через GitOps + restart.
    // ------------------------------------------------------------------------
    builder.AddOpenTelemetryOptions();
    builder.AddCustomOpenTelemetry();

    // ------------------------------------------------------------------------
    // 4. БЕЗОПАСНОСТЬ (JWT аутентификация + policy-based авторизация)
    // Роли: Admin (изменение), Operator (чтение), Auditor (только чтение).
    // ------------------------------------------------------------------------
    builder.AddCustomAuthentication();
    builder.AddCustomAuthorization();

    // ------------------------------------------------------------------------
    // 5. API VERSIONING
    // Версионирование URL-based: /api/v1/logging/level
    // ReportApiVersions: заголовок api-supported-versions в ответе
    // ------------------------------------------------------------------------
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    // ------------------------------------------------------------------------
    // 6. CORS
    // Текущая политика: AllowAll (для разработки).
    // TODO: ограничить origins для production.
    // ------------------------------------------------------------------------
    builder.Services.AddCustomCors();

    // ------------------------------------------------------------------------
    // 7. EXCEPTION HANDLER
    // Перехватывает все необработанные исключения.
    // Возвращает единообразный JSON: {"error": {"code": 400, "message": "..."}}
    // ------------------------------------------------------------------------
    builder.Services.AddCustomExceptionHandler();

    // ------------------------------------------------------------------------
    // 8. CORRELATION ID
    // Генерирует X-Correlation-Id (Guid.CreateVersion7()) или прокидывает
    // входящий. Добавляется в LogContext и Activity для трассировки.
    // ------------------------------------------------------------------------
    builder.Services.AddCustomCorrelationId();

    // ------------------------------------------------------------------------
    // 9. REQUEST/RESPONSE LOGGING
    // Логирует method, path, status code, duration.
    // Не логирует тела request/response.
    // ------------------------------------------------------------------------
    builder.Services.AddCustomRequestResponseLogging();

    // ------------------------------------------------------------------------
    // 10. HEALTH CHECKS
    // Регистрация health checks в DI.
    // Liveness: всегда Healthy (игнорирует shutdown).
    // Readiness: Unhealthy при shutdown (через IHostApplicationLifetime).
    // ------------------------------------------------------------------------
    builder.Services.AddCustomHealthChecks();

    // ------------------------------------------------------------------------
    // 11. RATE LIMITING
    // Ограничение количества запросов к health-эндпоинтам.
    // ------------------------------------------------------------------------
    builder.Services.AddCustomRateLimiting();

    // ------------------------------------------------------------------------
    // 11.1. RESPONSE CACHING
    // Кэширование HTTP-ответов на уровне middleware.
    // Используется атрибутом [ResponseCache] на endpoints.
    // ------------------------------------------------------------------------
    builder.Services.AddResponseCaching();

    // ------------------------------------------------------------------------
    // 12. OPENAPI + SCALAR UI
    // Генерация OpenAPI-документа и UI для тестирования API.
    // JWT Bearer авторизация добавлена через DocumentTransformer.
    // ------------------------------------------------------------------------
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            var meta = context.ApplicationServices
                .GetRequiredService<IOptions<ApiMetadataOptions>>().Value;

            document.Info.Title = meta.Title;
            document.Info.Version = meta.Version;
            document.Info.Description = meta.Description;

            document.Info.Contact = new OpenApiContact
            {
                Name = meta.Developer.Name,
                Email = meta.Developer.Email,
                Url = new Uri(meta.Developer.Url)
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT токен для авторизации"
                }
            };

            document.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
                }
            };

            return Task.CompletedTask;
        });
    });

    // ========================================================================
    // ПОСТРОЕНИЕ PIPELINE
    // ========================================================================
    var app = builder.Build();

    // ------------------------------------------------------------------------
    // MIDDLEWARE PIPELINE — порядок важен!
    // ------------------------------------------------------------------------

    // 1. Exception Handler — ПЕРВЫМ, перехватывает все исключения
    app.UseCustomExceptionHandler();

    // 2. CORS — до всех остальных middleware
    app.UseCors();

    // 3. Correlation ID — после CORS, до логирования
    app.UseCustomCorrelationId();

    // 4. Request/Response Logging — после Correlation ID
    app.UseCustomRequestResponseLogging();

    // 5. Authentication & Authorization — до кэша и rate limiter
    app.UseAuthentication();
    app.UseAuthorization();

    // 6. Response Caching — после Auth, чтобы не кэшировать 401/403
    app.UseResponseCaching();

    // 7. Rate Limiting — до endpoints, чтобы ограничивать запросы
    app.UseRateLimiter();

    // 8. Controllers
    app.MapControllers();

    // 9. Metadata endpoint — публичный, кэшируется на 1 час
    app.MapGet("/api/metadata", (IOptions<ApiMetadataOptions> meta) =>
    {
        var m = meta.Value;
        return Results.Ok(new
        {
            m.Title,
            m.Version,
            m.Description,
            Developer = new
            {
                m.Developer.Name,
                m.Developer.Email,
                m.Developer.Url
            }
        });
    })
    .WithName("GetApiMetadata")
    .WithTags("Metadata")
    .WithMetadata(new ResponseCacheAttribute { Duration = 3600 })
    .AllowAnonymous();

    // 10. OpenAPI + Scalar UI + Dev endpoints — только в Development
    if (app.Environment.IsDevelopment())
    {
        var meta = app.Services.GetRequiredService<IOptions<ApiMetadataOptions>>().Value;

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle(meta.Title)
                .WithTheme(ScalarTheme.Saturn);
        });

        // Dev endpoint — генерация тестовых JWT-токенов (только Development)
        app.MapPost("/dev/token", (DevTokenRequest req) =>
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("name", req.Username),
                new System.Security.Claims.Claim("sub", req.Username)
            };

            foreach (var role in req.Roles)
            {
                claims.Add(new System.Security.Claims.Claim("role", role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = builder.Configuration["Jwt:Issuer"] ?? "WebApiTemplate",
                Audience = builder.Configuration["Jwt:Audience"] ?? "WebApiTemplate",
                Subject = new System.Security.Claims.ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = creds
            };

            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);

            return Results.Ok(new { token, expires = 8 });
        }).ExcludeFromDescription();
    }

    // 11. Health Checks — привязка к порту 8081
    app.UseCustomHealthChecks();

    // ========================================================================
    // ЗАПУСК ХОСТА
    // ========================================================================
    await app.RunAsync();
}
catch (Exception ex)
{
    // Дублируем в Console — Serilog может не успеть записать
    Console.WriteLine($"[FATAL] Host terminated unexpectedly: {ex}");
    Log.Fatal(ex, "Host terminated unexpectedly");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}

// ============================================================================
// ОБЪЯВЛЕНИЯ ТИПОВ — СТРОГО В КОНЦЕ ФАЙЛА!
// ============================================================================
public record DevTokenRequest(string Username, string[] Roles);

// ============================================================================
// ПОЛУЧАСТЬ ДЛЯ WebApplicationFactory<Program>
// ============================================================================
public partial class Program { }