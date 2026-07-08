# Firefly AI Flow Platform — Web API Template

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

## Что включено

| Компонент              | Описание                                                            |
| ---------------------- | ------------------------------------------------------------------- |
| **Clean Architecture** | Controllers → Services → Repositories                               |
| **Result Pattern**     | Типобезопасная обработка ошибок через `Fafp.Shared.ResultPattern`   |
| **Serilog**            | Структурированное логирование (JSON в prod, человекочитаемое в dev) |
| **OpenTelemetry**      | Трейсинг, метрики, логи → OTLP endpoint                             |
| **JWT Authentication** | Bearer-токены, policy-based авторизация                             |
| **Health Checks**      | Liveness + Readiness на отдельном порту (8081)                      |
| **CORS**               | Настраиваемая политика                                              |
| **Rate Limiting**      | Ограничение запросов к health endpoints                             |
| **Correlation ID**     | Сквозной X-Correlation-Id (Guid v7)                                 |
| **OpenAPI + Scalar**   | Scalar UI с JWT-авторизацией                                        |
| **API Versioning**     | URL-based versioning (/api/v1/...)                                  |
| **202 теста**          | Unit + Integration через TUnit + WebApplicationFactory              |

## Структура проекта

```bash
MyService/
├── MyService/
│   ├── Configuration/Options/     ← Options pattern
│   ├── Contracts/                 ← DTO, запросы, валидаторы
│   ├── Controllers/               ← API endpoints
│   ├── Extensions/                ← Middleware, DI расширения
│   ├── HealthChecks/              ← Liveness + Readiness
│   ├── Security/                  ← AuthN + AuthZ
│   ├── Program.cs                 ← Точка входа
│   └── appsettings*.json          ← Конфигурация
├── MyService.Tests/
│   ├── Configuration/
│   ├── Contracts/
│   ├── Controllers/
│   ├── Extensions/
│   ├── HealthChecks/
│   ├── Integration/               ← E2E тесты
│   ├── Security/
│   └── Helpers/
├── MyService.slnx
└── global.json
```

## Endpoints

| Endpoint                | Метод | Описание                                     |
| ----------------------- | ----- | -------------------------------------------- |
| `/api/v1/logging/level` | GET   | Получить текущий уровень логирования         |
| `/api/v1/logging/level` | PUT   | Изменить уровень логирования (runtime)       |
| `/api/metadata`         | GET   | Информация о сервисе (кэшируется 1 час)      |
| `/health`               | GET   | Liveness probe (порт 8081)                   |
| `/health/ready`         | GET   | Readiness probe (порт 8081)                  |
| `/dev/token`            | POST  | Генерация тестового JWT (только Development) |

## Конфигурация

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

## Тестирование

```bash
cd MyService.Tests
dotnet test
```

**Результат:** 202 теста (unit + integration), покрытие через coverlet.

## Зависимости

- `Fafp.Shared.ResultPattern` 1.0.0 — типобезопасная обработка ошибок
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
