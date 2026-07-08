# Firefly AI Flow Platform — Шаблон Web API

[🇬🇧 English](README.md) | [🇷🇺 Русский](README.ru.md)

Production-ready шаблон для создания ASP.NET Core 10 микросервисов.

---

![ASP.NET](https://img.shields.io/badge/ASP.NET-10-512BD4?style=flat&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13-239120?style=flat&logo=csharp)
![License](https://img.shields.io/badge/license-MIT-blue.svg)

---

## Быстрый старт

```bash
# Установить шаблон
dotnet new install Faf.Templates.WebApi

# Создать проект
dotnet new faf-webapi -n MyService

# Собрать и запустить
cd MyService
dotnet run
```

## Параметры шаблона

| Параметр             | По умолчанию                        | Описание                                |
| -------------------- | ----------------------------------- | --------------------------------------- |
| `-n, --name`         | (имя папки)                         | Имя проекта (заменяет `WebApiTemplate`) |
| `--Port`             | `8080`                              | HTTP порт сервиса                       |
| `--Author`           | `Kotrecon`                          | Имя автора                              |
| `--AuthorEmail`      | `ermakov_k@mail.ru`                 | Email автора                            |
| `--AuthorUrl`        | `https://github.com/Kotrecon`       | URL автора (профиль GitHub)             |
| `--Title`            | `Web API Service`                   | Заголовок сервиса (для OpenAPI/Scalar)  |
| `--Version`          | `1.0.0`                             | Версия сервиса (semver)                 |
| `--Description`      | `ASP.NET Core Web API microservice` | Описание сервиса                        |
| `--ServiceHost`      | `api.example.com`                   | Хост сервиса (для nginx example)        |
| `-I, --IncludeTests` | `true`                              | Включить тестовый проект                |

**Пример со всеми параметрами:**

```bash
dotnet new faf-webapi -n OrderService \
    --Port 8082 \
    --Author "Иван Петров" \
    --AuthorEmail "ivan@example.com" \
    --Title "Order Service API" \
    --Version "1.0.0" \
    --Description "Сервис обработки заказов" \
    --ServiceHost "orders.mycompany.com"
```

## Что включено

| Компонент              | Описание                                                            |
| ---------------------- | ------------------------------------------------------------------- |
| **Clean Architecture** | Controllers → Services → Repositories                               |
| **Result Pattern**     | Типобезопасная обработка ошибок через `Fafp.ResultPattern`          |
| **Serilog**            | Структурированное логирование (JSON в prod, человекочитаемое в dev) |
| **OpenTelemetry**      | Трейсинг, метрики, логи → OTLP endpoint                             |
| **JWT Authentication** | Bearer-токены, policy-based авторизация                             |
| **Health Checks**      | Liveness + Readiness на отдельном порту (8081)                      |
| **CORS**               | Настраиваемая политика                                              |
| **Rate Limiting**      | Ограничение запросов к health endpoints                             |
| **Correlation ID**     | Сквозной X-Correlation-Id (Guid v7)                                 |
| **OpenAPI + Scalar**   | Scalar UI с JWT-авторизацией                                        |
| **API Versioning**     | URL-based versioning (/api/v1/...)                                  |
| **Response Caching**   | HTTP-кэширование для публичных endpoints                            |
| **202 теста**          | Unit + Integration через TUnit + WebApplicationFactory              |

## Структура проекта

```bash
webapi/
├── .git/
├── .gitignore
├── .template.config/
│   └── template.json
├── docs/
│   ├── architecture/
│   │   ├── adr.md
│   │   ├── adr.ru.md
│   │   ├── api.md
│   │   ├── api.ru.md
│   │   ├── architecture.md
│   │   ├── architecture.ru.md
│   │   ├── auth-flow.md
│   │   ├── auth-flow.ru.md
│   │   ├── deployment.md
│   │   ├── deployment.ru.md
│   │   ├── observability.md
│   │   ├── observability.ru.md
│   │   ├── operability.md
│   │   └── operability.ru.md
│   ├── configuration.md
│   ├── configuration.ru.md
│   ├── index.md
│   ├── index.ru.md
│   ├── quickstart.md
│   ├── quickstart.ru.md
│   ├── roadmap.md
│   ├── roadmap.ru.md
│   ├── testing.md
│   └── testing.ru.md
├── Faf.Templates.WebApi.csproj
├── global.json
├── icon.png
├── LICENSE
├── README.md
├── README.ru.md
├── WebApiTemplate.slnx
│
├── WebApiTemplate/
│   ├── Configuration/Options/
│   │   ├── ApiMetadataOptions.cs
│   │   ├── AppSettings.cs
│   │   ├── ContactInfo.cs
│   │   ├── JwtOptions.cs
│   │   └── OpenTelemetryOptions.cs
│   ├── Contracts/Dto/Request/Logging/
│   │   ├── SetLogLevelRequest.cs
│   │   └── SetLogLevelValidator.cs
│   ├── Controllers/
│   │   └── LoggingController.cs
│   ├── Extensions/
│   │   ├── ConfigurationExtensions.cs
│   │   ├── ObservabilityExtensions.cs
│   │   ├── CorrelationId/
│   │   │   ├── CorrelationIdExtensions.cs
│   │   │   └── CorrelationIdMiddleware.cs
│   │   ├── Cors/
│   │   │   └── CorsExtensions.cs
│   │   ├── ExceptionHandler/
│   │   │   ├── ExceptionHandlerExtensions.cs
│   │   │   └── ExceptionHandlerMiddleware.cs
│   │   ├── HealthChecks/
│   │   │   └── HealthCheckExtensions.cs
│   │   ├── RateLimiting/
│   │   │   └── RateLimitingExtensions.cs
│   │   └── RequestResponseLogging/
│   │       ├── RequestResponseLoggingExtensions.cs
│   │       └── RequestResponseLoggingMiddleware.cs
│   ├── HealthChecks/
│   │   ├── DatabaseHealthChecker.cs
│   │   ├── IDatabaseHealthChecker.cs
│   │   ├── MinimalResponseWriter.cs
│   │   └── ReadinessHealthCheck.cs
│   ├── Security/
│   │   ├── AuthenticationExtensions.cs
│   │   └── AuthorizationExtensions.cs
│   ├── Program.cs
│   ├── WebApiTemplate.csproj
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── appsettings.Production.json
│
└── WebApiTemplate.Tests/
    ├── Configuration/Options/
    │   ├── ApiMetadataOptionsTests.cs
    │   ├── AppSettingsTests.cs
    │   ├── ContactInfoTests.cs
    │   ├── JwtOptionsTests.cs
    │   └── OpenTelemetryOptionsTests.cs
    ├── Contracts/Dto/
    │   └── SetLogLevelRequestTests.cs
    ├── Controllers/
    │   ├── ApiVersioningTests.cs
    │   └── LoggingControllerTests.cs
    ├── Extensions/
    │   ├── ConfigurationExtensionsTests.cs
    │   ├── ObservabilityExtensionsTests.cs
    │   ├── CorrelationId/
    │   │   └── CorrelationIdMiddlewareTests.cs
    │   ├── Cors/
    │   │   └── CorsExtensionsTests.cs
    │   ├── ExceptionHandler/
    │   │   └── ExceptionHandlerMiddlewareTests.cs
    │   └── RequestResponseLogging/
    │       └── RequestResponseLoggingMiddlewareTests.cs
    ├── HealthChecks/
    │   ├── MinimalResponseWriterTests.cs
    │   └── ReadinessHealthCheckTests.cs
    ├── Helpers/
    │   └── RecursiveValidator.cs
    ├── Integration/
    │   ├── Infrastructure/
    │   │   └── TestWebApplicationFactory.cs
    │   ├── AuthenticationTests.cs
    │   ├── AuthorizationTests.cs
    │   ├── CorrelationIdE2ETests.cs
    │   ├── DevTokenEndpointTests.cs
    │   └── MetadataEndpointTests.cs
    ├── Security/
    │   ├── AuthenticationExtensionsTests.cs
    │   └── AuthorizationExtensionsTests.cs
    ├── WebApiTemplate.Tests.csproj
    └── coverlet.runsettings
```

## Endpoints

### API (порт 8080)

| Endpoint                     | Метод | Описание                                     | Доступ      |
| ---------------------------- | ----- | -------------------------------------------- | ----------- |
| `/api/v1/logging/level`      | GET   | Получить текущий уровень логирования         | AuditViewer |
| `/api/v1/logging/level`      | PUT   | Изменить уровень логирования (runtime)       | AdminOnly   |
| `/api/v1/logging/categories` | GET   | Список категорий с overrides                 | AuditViewer |
| `/api/metadata`              | GET   | Информация о сервисе (кэш 1 час)             | Анонимный   |
| `/dev/token`                 | POST  | Генерация тестового JWT (только Development) | Анонимный   |

### Health Checks (порт 8081 — только внутренняя сеть)

| Endpoint        | Метод | Описание              | Доступ     |
| --------------- | ----- | --------------------- | ---------- |
| `/health/live`  | GET   | Liveness probe        | Внутренний |
| `/health/ready` | GET   | Readiness probe       | Внутренний |
| `/health`       | GET   | Агрегированный статус | Внутренний |

### OpenAPI (только Development)

| URL                                     | Описание                               |
| --------------------------------------- | -------------------------------------- |
| `http://localhost:8080/scalar/v1`       | Scalar UI (интерактивная документация) |
| `http://localhost:8080/openapi/v1.json` | OpenAPI 3.1 документ                   |

## Конфигурация

### appsettings.json

```json
{
  "Kestrel": {
    "Endpoints": {
      "Api": { "Url": "http://0.0.0.0:8080" },
      "Health": { "Url": "http://0.0.0.0:8081" }
    }
  },
  "AppSettings": {
    "ServiceName": "MyService",
    "Port": 8080
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyAtLeast32CharactersLong!",
    "Issuer": "MyService",
    "Audience": "MyService"
  },
  "OpenTelemetry": {
    "Endpoint": "http://otel-collector:4317",
    "Protocol": "Grpc",
    "Headers": {},
    "UseConsoleExporter": false
  },
  "ApiMetadata": {
    "Title": "SERVICE_TITLE_PLACEHOLDER",
    "Version": "SERVICE_VERSION_PLACEHOLDER",
    "Description": "SERVICE_DESCRIPTION_PLACEHOLDER",
    "Developer": {
      "Name": "AUTHOR_PLACEHOLDER",
      "Email": "AUTHOR_EMAIL_PLACEHOLDER",
      "Url": "AUTHOR_URL_PLACEHOLDER"
    }
  }
}
```

### Переменные окружения

| Переменная                | Обязательно | Описание                                              |
| ------------------------- | ----------- | ----------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`  | Да          | `Production` / `Development`                          |
| `Jwt__Key`                | Да          | JWT ключ подписи (минимум 32 символа)                 |
| `OpenTelemetry__Endpoint` | Нет         | OTLP endpoint (по умолчанию: `http://localhost:4317`) |

## Тестирование

```bash
cd MyService.Tests
dotnet test
```

**Результат:** 202 теста (unit + integration), покрытие через coverlet.

## Зависимости

- `Fafp.ResultPattern` 1.0.0 — типобезопасная обработка ошибок
- `Serilog.AspNetCore` — структурированное логирование
- `OpenTelemetry.*` — трейсинг и метрики
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT
- `Scalar.AspNetCore` — OpenAPI UI
- `TUnit` — тестовый фреймворк

---

## Roadmap

✅ v1.0.0 — Базовый шаблон

- [x] Clean Architecture
- [x] Result Pattern интеграция
- [x] Serilog + OpenTelemetry
- [x] JWT + Health Checks
- [x] 202 теста (TUnit)
- [x] NuGet-пакет шаблона

📋 v1.1.0 — Docker & DevOps

- [ ] `Dockerfile` (multi-stage build)
- [ ] `docker-compose.yml` (сервис + PostgreSQL + OTel Collector)
- [ ] `.dockerignore`
- [ ] GitHub Actions CI/CD workflow

---

## Автор

**AUTHOR_PLACEHOLDER**

AUTHOR_DESC_RU_PLACEHOLDER

[Email](mailto:AUTHOR_EMAIL_PLACEHOLDER) | [GitHub](AUTHOR_URL_PLACEHOLDER)

---

## Лицензия

MIT

---
