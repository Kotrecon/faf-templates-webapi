# 🔥 FAF Web API Template

Production-ready шаблон для создания ASP.NET Core 10 микросервисов.

## Быстрый старт

```bash
dotnet new install Faf.Templates.WebApi
dotnet new faf-webapi -n MyService
cd MyService
dotnet run
```

## Что внутри

- Clean Architecture
- Result Pattern (типобезопасные ошибки)
- Serilog + OpenTelemetry
- JWT Authentication
- Health Checks (Liveness + Readiness)
- 202 теста (TUnit)

## Навигация

- [Быстрый старт](quickstart.md)
- [Конфигурация](configuration.md)
- [Тестирование](testing.md)
- [Roadmap](roadmap.md)
