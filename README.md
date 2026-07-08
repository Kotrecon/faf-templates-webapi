# Firefly AI Flow Platform — Web API Template

[🇬🇧 English](README.md) | [🇷🇺 Русский](README.ru.md)

Production-ready template for creating ASP.NET Core 10 microservices.

---

![ASP.NET](https://img.shields.io/badge/ASP.NET-10-512BD4?style=flat&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13-239120?style=flat&logo=csharp)
![License](https://img.shields.io/badge/license-MIT-blue.svg)

---

## Quick Start

```bash
# Install template
dotnet new install Fafp.Templates.WebApi

# Create project
dotnet new faf-webapi -n MyService

# Build and run
cd MyService
dotnet run
```

## Template Parameters

| Parameter            | Default                             | Description                              |
| -------------------- | ----------------------------------- | ---------------------------------------- |
| `-n, --name`         | (folder name)                       | Project name (replaces `WebApiTemplate`) |
| `--Port`             | `8080`                              | HTTP port for the service                |
| `--Author`           | `Kotrecon`                          | Author name                              |
| `--AuthorEmail`      | `ermakov_k@mail.ru`                 | Author email                             |
| `--AuthorUrl`        | `https://github.com/Kotrecon`       | Author URL (GitHub profile)              |
| `--Title`            | `Web API Service`                   | Service title (used in OpenAPI/Scalar)   |
| `--Version`          | `1.0.0`                             | Service version (semver)                 |
| `--Description`      | `ASP.NET Core Web API microservice` | Service description                      |
| `--ServiceHost`      | `api.example.com`                   | Service host (used in nginx example)     |
| `-I, --IncludeTests` | `true`                              | Include test project                     |

**Example with all parameters:**

```bash
dotnet new faf-webapi -n OrderService \
    --Port 8082 \
    --Author "John Doe" \
    --AuthorEmail "john@example.com" \
    --Title "Order Service API" \
    --Version "1.0.0" \
    --Description "Service for processing orders" \
    --ServiceHost "orders.mycompany.com"
```

## What's Included

| Component              | Description                                              |
| ---------------------- | -------------------------------------------------------- |
| **Clean Architecture** | Controllers → Services → Repositories                    |
| **Result Pattern**     | Type-safe error handling via `Fafp.ResultPattern`        |
| **Serilog**            | Structured logging (JSON in prod, human-readable in dev) |
| **OpenTelemetry**      | Tracing, metrics, logs → OTLP endpoint                   |
| **JWT Authentication** | Bearer tokens, policy-based authorization                |
| **Health Checks**      | Liveness + Readiness on separate port (8081)             |
| **CORS**               | Configurable policy                                      |
| **Rate Limiting**      | Request limiting for health endpoints                    |
| **Correlation ID**     | End-to-end X-Correlation-Id (Guid v7)                    |
| **OpenAPI + Scalar**   | Scalar UI with JWT authorization                         |
| **API Versioning**     | URL-based versioning (/api/v1/...)                       |
| **Response Caching**   | HTTP caching for public endpoints                        |
| **202 tests**          | Unit + Integration via TUnit + WebApplicationFactory     |

## Project Structure

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

### API (port 8080)

| Endpoint                     | Method | Description                          | Access      |
| ---------------------------- | ------ | ------------------------------------ | ----------- |
| `/api/v1/logging/level`      | GET    | Get current logging level            | AuditViewer |
| `/api/v1/logging/level`      | PUT    | Change logging level (runtime)       | AdminOnly   |
| `/api/v1/logging/categories` | GET    | List categories with overrides       | AuditViewer |
| `/api/metadata`              | GET    | Service info (cached 1 hour)         | Anonymous   |
| `/dev/token`                 | POST   | Generate test JWT (Development only) | Anonymous   |

### Health Checks (port 8081 — internal only)

| Endpoint        | Method | Description       | Access   |
| --------------- | ------ | ----------------- | -------- |
| `/health/live`  | GET    | Liveness probe    | Internal |
| `/health/ready` | GET    | Readiness probe   | Internal |
| `/health`       | GET    | Aggregated status | Internal |

### OpenAPI (Development only)

| URL                                     | Description                  |
| --------------------------------------- | ---------------------------- |
| `http://localhost:8080/scalar/v1`       | Scalar UI (interactive docs) |
| `http://localhost:8080/openapi/v1.json` | OpenAPI 3.1 document         |

## Configuration

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

### Environment Variables

| Variable                  | Required | Description                                      |
| ------------------------- | -------- | ------------------------------------------------ |
| `ASPNETCORE_ENVIRONMENT`  | Yes      | `Production` / `Development`                     |
| `Jwt__Key`                | Yes      | JWT signing key (min 32 chars)                   |
| `OpenTelemetry__Endpoint` | No       | OTLP endpoint (default: `http://localhost:4317`) |

## Testing

```bash
cd MyService.Tests
dotnet test
```

**Result:** 202 tests (unit + integration), coverage via coverlet.

## Dependencies

- `Fafp.ResultPattern` 1.0.0 — type-safe error handling
- `Serilog.AspNetCore` — structured logging
- `OpenTelemetry.*` — tracing and metrics
- `Microsoft.AspNetCore.Authentication.JwtBearer` — JWT
- `Scalar.AspNetCore` — OpenAPI UI
- `TUnit` — testing framework

---

## Roadmap

✅ v1.0.0 — Base template

- [x] Clean Architecture
- [x] Result Pattern integration
- [x] Serilog + OpenTelemetry
- [x] JWT + Health Checks
- [x] 202 tests (TUnit)
- [x] NuGet template package

📋 v1.1.0 — Docker & DevOps

- [ ] `Dockerfile` (multi-stage build)
- [ ] `docker-compose.yml` (service + PostgreSQL + OTel Collector)
- [ ] `.dockerignore`
- [ ] GitHub Actions CI/CD workflow

---

## Author

**Kotrecon**

Solution Architect from Saint Petersburg. Specialization: .NET, C#, JS, Python, AI/ML, RAG, Agents, DevOps, GitHub, GitLab, CI/CD, Industrial Automation, Industrial Software, DB, PostgreSQL.

[Email](mailto:ermakov_k@mail.ru) | [GitHub](https://github.com/Kotrecon)

---

## License

MIT

---
