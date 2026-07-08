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
dotnet new install Faf.Templates.WebApi

# Create project
dotnet new faf-webapi -n MyService

# Build and run
cd MyService
dotnet run
```

## What's Included

| Component              | Description                                              |
| ---------------------- | -------------------------------------------------------- |
| **Clean Architecture** | Controllers → Services → Repositories                    |
| **Result Pattern**     | Type-safe error handling via `Fafp.Shared.ResultPattern` |
| **Serilog**            | Structured logging (JSON in prod, human-readable in dev) |
| **OpenTelemetry**      | Tracing, metrics, logs → OTLP endpoint                   |
| **JWT Authentication** | Bearer tokens, policy-based authorization                |
| **Health Checks**      | Liveness + Readiness on separate port (8081)             |
| **CORS**               | Configurable policy                                      |
| **Rate Limiting**      | Request limiting for health endpoints                    |
| **Correlation ID**     | End-to-end X-Correlation-Id (Guid v7)                    |
| **OpenAPI + Scalar**   | Scalar UI with JWT authorization                         |
| **API Versioning**     | URL-based versioning (/api/v1/...)                       |
| **202 tests**          | Unit + Integration via TUnit + WebApplicationFactory     |

## Project Structure

```bash
MyService/
├── MyService/
│   ├── Configuration/Options/     ← Options pattern
│   ├── Contracts/                 ← DTO, requests, validators
│   ├── Controllers/               ← API endpoints
│   ├── Extensions/                ← Middleware, DI extensions
│   ├── HealthChecks/              ← Liveness + Readiness
│   ├── Security/                  ← AuthN + AuthZ
│   ├── Program.cs                 ← Entry point
│   └── appsettings*.json          ← Configuration
├── MyService.Tests/
│   ├── Configuration/
│   ├── Contracts/
│   ├── Controllers/
│   ├── Extensions/
│   ├── HealthChecks/
│   ├── Integration/               ← E2E tests
│   ├── Security/
│   └── Helpers/
├── MyService.slnx
└── global.json
```

## Endpoints

| Endpoint                | Method | Description                          |
| ----------------------- | ------ | ------------------------------------ |
| `/api/v1/logging/level` | GET    | Get current logging level            |
| `/api/v1/logging/level` | PUT    | Change logging level (runtime)       |
| `/api/metadata`         | GET    | Service info (cached 1 hour)         |
| `/health`               | GET    | Liveness probe (port 8081)           |
| `/health/ready`         | GET    | Readiness probe (port 8081)          |
| `/dev/token`            | POST   | Generate test JWT (Development only) |

## Configuration

### appsettings.json

```json
{
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

## Testing

```bash
cd MyService.Tests
dotnet test
```

**Result:** 202 tests (unit + integration), coverage via coverlet.

## Dependencies

- `Fafp.Shared.ResultPattern` 1.0.0 — type-safe error handling
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

**AUTHOR_PLACEHOLDER**

AUTHOR_DESC_RU_PLACEHOLDER

[Email](mailto:AUTHOR_EMAIL_PLACEHOLDER) | [GitHub](AUTHOR_URL_PLACEHOLDER)

Solution Architect from St. Petersburg. Specialization: .NET, C#, JS, Python, AI/ML, RAG, Agents, DevOps, GitHub, GitLab, CI/CD, Industrial Automation, Industrial Software, DB, PostgreSQL.

---

## License

MIT

---
