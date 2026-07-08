# Архитектура — Текущее состояние

| Поле       | Значение                    |
| ---------- | --------------------------- |
| **Версия** | SERVICE_VERSION_PLACEHOLDER |
| **Статус** | Active                      |
| **Дата**   | 2026-07-08                  |

---

## Стек технологий

| Компонент      | Технология                                    | Версия  |
| -------------- | --------------------------------------------- | ------- |
| Runtime        | .NET                                          | 10.0    |
| SDK            | Microsoft.NET.Sdk.Web                         | —       |
| DI             | Microsoft.Extensions.DependencyInjection      | 10.0.9  |
| Hosting        | Microsoft.Extensions.Hosting                  | 10.0.9  |
| Configuration  | Microsoft.Extensions.Configuration.Json       | 10.0.9  |
| Logging        | Serilog.AspNetCore                            | 10.0.0  |
| Telemetry      | OpenTelemetry.Extensions.Hosting              | 1.16.0  |
| Auth           | Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.9  |
| Token Gen      | System.IdentityModel.Tokens.Jwt               | 8.19.1  |
| API Versioning | Asp.Versioning.Mvc                            | 10.0.0  |
| OpenAPI        | Microsoft.AspNetCore.OpenApi                  | 10.0.9  |
| API UI         | Scalar.AspNetCore                             | 2.13.19 |
| Testing        | TUnit                                         | 1.56.35 |
| Mocking        | Moq                                           | 4.20.72 |
| Coverage       | Microsoft.Testing.Extensions.CodeCoverage     | 18.8.0  |
| Response Cache | Microsoft.AspNetCore.ResponseCaching          | 10.0.9  |
| Result Pattern | Faf.Shared.ResultPattern                      | 1.0.0   |
| ORM            | EF Core (планируется)                         | —       |
| Cache          | Redis (планируется)                           | —       |
| Message Bus    | RabbitMQ / Kafka (планируется)                | —       |

---

## Структура проекта

```bash
MyService/
├── docs/architecture/
│   ├── adr.md
│   ├── api.md
│   ├── architecture.md
│   ├── auth-flow.md
│   ├── deployment.md
│   ├── observability.md
│   └── operability.md
├── MyService/
│   ├── Configuration/Options/
│   │   ├── ApiMetadataOptions.cs
│   │   ├── AppSettings.cs
│   │   ├── ContactInfo.cs
│   │   ├── JwtOptions.cs
│   │   └── OpenTelemetryOptions.cs
│   ├── Contracts/Dto/
│   │   ├── Request/
│   │   └── Response/
│   ├── Controllers/
│   │   └── LoggingController.cs
│   ├── Extensions/
│   │   ├── ConfigurationExtensions.cs
│   │   ├── CorrelationId/
│   │   ├── Cors/
│   │   ├── ExceptionHandler/
│   │   ├── HealthChecks/
│   │   ├── ObservabilityExtensions.cs
│   │   ├── RateLimiting/
│   │   └── RequestResponseLogging/
│   ├── HealthChecks/
│   │   ├── DatabaseHealthChecker.cs
│   │   ├── IDatabaseHealthChecker.cs
│   │   ├── MinimalResponseWriter.cs
│   │   └── ReadinessHealthCheck.cs
│   ├── Security/
│   │   ├── AuthenticationExtensions.cs
│   │   └── AuthorizationExtensions.cs
│   ├── Program.cs
│   ├── MyService.csproj
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.Production.json
├── MyService.Tests/
│   ├── Configuration/
│   ├── Contracts/
│   ├── Controllers/
│   ├── Extensions/
│   ├── HealthChecks/
│   ├── Helpers/
│   ├── Integration/
│   ├── Security/
│   ├── MyService.Tests.csproj
│   └── coverlet.runsettings
├── MyService.slnx
└── global.json
```

---

## Регистрация сервисов (порядок в Program.cs)

> Это основной источник истины для порядка регистрации и middleware pipeline.

### DI Container (порядок регистрации)

```csharp
1.  builder.AddCustomLogging() — Serilog + LoggingLevelSwitch
2.  builder.AddAppSettings() — IOptions<AppSettings> + ValidateOnStart
3.  builder.AddJwt() — IOptions<JwtOptions> + ValidateOnStart
4.  builder.AddApiMetadata() — IOptions<ApiMetadataOptions> + ValidateOnStart
5.  builder.AddOpenTelemetryOptions() — IOptions<OpenTelemetryOptions> + ValidateOnStart
6.  builder.AddCustomOpenTelemetry() — OTel logs/traces/metrics pipeline
7.  builder.AddCustomAuthentication() — JWT Bearer
8.  builder.AddCustomAuthorization() — Policy-based
9.  builder.Services.AddApiVersioning() — URL-based versioning (v1)
10. builder.Services.AddCustomCors() — CORS (AllowAll для разработки)
11. builder.Services.AddCustomExceptionHandler() — Exception Handler
12. builder.Services.AddCustomCorrelationId() — Correlation ID (Guid.CreateVersion7)
13. builder.Services.AddCustomRequestResponseLogging() — Request/Response logging
14. builder.Services.AddCustomHealthChecks() — health checks DI
15. builder.Services.AddCustomRateLimiting() — rate limiter DI
16. builder.Services.AddResponseCaching() — response caching
17. builder.Services.AddOpenApi() — OpenAPI 3.1 документ + JWT Bearer схема
```

### Middleware Pipeline (порядок выполнения)

```csharp
18. app.UseCustomExceptionHandler() — Exception Handler (ПЕРВЫМ middleware)
19. app.UseCors() — CORS middleware
20. app.UseCustomCorrelationId() — Correlation ID middleware
21. app.UseCustomRequestResponseLogging() — Request/Response logging middleware
22. app.UseAuthentication() — JWT authentication
23. app.UseAuthorization() — policy-based authorization
24. app.UseResponseCaching() — response caching
25. app.UseRateLimiter() — rate limiter middleware
26. app.MapControllers() — MVC pipeline
27. app.MapGet("/api/metadata") — metadata endpoint [AllowAnonymous]
28. app.MapOpenApi() — OpenAPI endpoint (/openapi/v1.json) [Development only]
29. app.MapScalarApiReference() — Scalar UI (/scalar/v1) [Development only]
30. app.MapPost("/dev/token") — Dev token endpoint [Development only]
31. app.UseCustomHealthChecks() — health endpoints на порту 8081
```

---

## OpenAPI + Scalar UI

### Назначение

Интерактивная документация и тестирование API в окружении Development.

### Endpoints

| URL                | Описание                               |
| ------------------ | -------------------------------------- |
| `/openapi/v1.json` | OpenAPI 3.1 документ                   |
| `/scalar/v1`       | Scalar UI — интерактивная документация |

### Особенности

- Включено только в `Development`
- JWT Bearer схема встроена в документ (кнопка Authorize в UI)
- XML-комментарии из кода автоматически включаются в документацию
- Source generator генерирует трансформеры для OpenAPI-документа
- Метаданные (title, version, description, developer) берутся из `ApiMetadataOptions` — единый источник с `/api/metadata`

---

## Metadata API

### Назначение

Публичный endpoint с метаданными API. Используется фронтендом для отображения версии API, footer, about-страницы.

### Endpoint

| URL             | Описание              | Доступ    |
| --------------- | --------------------- | --------- |
| `/api/metadata` | Метаданные API (JSON) | Анонимный |

### Особенности

- Не требует аутентификации (`AllowAnonymous`)
- Кэшируется на 1 час (`ResponseCache(Duration = 3600)`)
- Источник данных: `ApiMetadataOptions` (appsettings.json)
- Единый источник с OpenAPI/Scalar — те же метаданные

---

## Безопасность

| Компонент      | Реализация                                      |
| -------------- | ----------------------------------------------- |
| Аутентификация | JWT Bearer                                      |
| Авторизация    | Policy-based (AdminOnly, Operator, AuditViewer) |
| Roles          | Admin, Operator, Auditor                        |
| Issuer         | WebApiTemplate                                  |
| Audience       | WebApiTemplate                                  |
| ClockSkew      | 1 минута                                        |
| Token Gen      | JsonWebTokenHandler (Microsoft.IdentityModel)   |
| Dev Endpoint   | /dev/token (только Development)                 |
| Metadata API   | /api/metadata (публичный, без секретов)         |

---

## Среды

| Параметр      | Production                                 | Development                                |
| ------------- | ------------------------------------------ | ------------------------------------------ |
| Log level     | Information                                | Debug                                      |
| Console sink  | false                                      | true                                       |
| OTel endpoint | `http://otel-collector:4317`               | `http://localhost:4317`                    |
| JWT Key       | YourSuperSecretKeyAtLeast32CharactersLong! | YourSuperSecretKeyAtLeast32CharactersLong! |
| OpenAPI UI    | Отключено                                  | Включено (/scalar/v1)                      |
| Dev Endpoint  | Отключено                                  | Включено (/dev/token)                      |
| ApiMetadata   | Production значения                        | Development значения                       |

---
