# ADR — Architecture Decision Records

| Поле       | Значение                    |
| ---------- | --------------------------- |
| **Версия** | SERVICE_VERSION_PLACEHOLDER |
| **Статус** | Active                      |
| **Дата**   | 2026-07-08                  |

> Архитектурные решения, принятые в проекте. Каждое решение фиксирует контекст, решение и последствия.

---

## ADR-001: Web SDK вместо Console SDK

**Статус:** Принято

**Контекст:** Проект был начат как консольный (Microsoft.NET.Sdk), но требовался ASP.NET Core для API, controllers, JWT auth.

**Решение:** Переключиться на `Microsoft.NET.Sdk.Web`.

**Последствия:**

- Доступны AddControllers, MapControllers, middleware pipeline
- Kestrel встроен
- Дополнительные пакеты не нужны (`Microsoft.AspNetCore.*` уже в SDK)

---

## ADR-002: Serilog через appsettings.json

**Статус:** Принято

**Контекст:** Serilog можно настраивать программно (WriteTo.Console) или через конфигурацию (ReadFrom.Configuration).

**Решение:** Стандартный формат Serilog в appsettings.json + ReadFrom.Configuration.

**Последствия:**

- Sinks настраиваются через JSON (не код)
- Добавление/удаление sinks без перекомпиляции
- Enricherse конфигурации

---

## ADR-003: LoggingLevelSwitch в DI

**Статус:** Принято

**Контекст:** Уровни логирования должны меняться на лету через API без restart.

**Решение:** LoggingLevelSwitch зарегистрирован как singleton в DI, контроллер получает его через constructor injection.

**Последствия:**

- Динамическое изменение уровней через PUT /api/logging/level
- Нет restart для смены уровня
- Fatal и Verbose запрещены (безопасность)

---

## ADR-004: Policy-based авторизация

**Статус:** Принято

**Контекст:** Нужно разграничить доступ: Admin (изменение), Operator (чтение), Auditor (только чтение).

**Решение:** Policy-based авторизация с тремя политиками: AdminOnly, Operator, AuditViewer.

**Последствия:**

- Гибкость: можно добавить policy без изменения кода контроллера
- Тестируемость: policies можно проверить через IAuthorizationService
- Читаемость: `[Authorize(Policy = "AdminOnly")]` вместо `[Authorize(Roles = "Admin")]`
- **Важно:** См. [`auth-flow.md`](./auth-flow.md) — `[Authorize]` на классе + методе = AND-логика. Классовый атрибут требует прохождения для каждого метода

---

## ADR-005: Health checks на отдельном порту

**Статус:** Принято

**Контекст:** Health checks должны быть доступны для оркестратора (Kubelet, Prometheus), но не доступны извне.

**Решение:** Два Kestrel-эндпоинта: API (8080) и Health (8081). Health-порт не маршрутизируется наружу (firewall / Nginx / Cloud LB).

**Последствия:**

- Middleware (logging, correlation, Scalar, CORS) автоматически исключены из health
- Сетевая изоляция вместо секретных заголовков
- Проще настраивать firewall / Nginx

---

## ADR-006: Rate limiting на health-ветку

**Статус:** Принято

**Контекст:** Даже с сетевой изоляцией, при утечке учётных данных или DDoS-атаке health-чеки могут перегрузить БД.

**Решение:** Fixed window limiter: 30 запросов за 10 секунд на `/health/*`.

**Последствия:**

- Дополнительная мера защиты (не основная)
- Kubelet (5-10 сек) + Prometheus (15-30 сек) + 3 эндпоинта = ~5-6 запросов за 10 сек
- Лимит 30 — с запасом

---

## ADR-007: CORS AllowAll для разработки

**Статус:** Принято (временно)

**Контекст:** Фронтенд и API могут работать на разных портах/доменах во время разработки. Нужно разрешить кросс-доменные запросы.

**Решение:** AllowAll политика (`*` origins, `*` methods, `*` headers) для разработки. В production будет ограничена.

**Последствия:**

- Удобно для локальной разработки
- Не безопасно для production (нужно ограничить origins)
- Конфигурация в appsettings.json, политика в CorsExtensions.cs

---

## ADR-008: Correlation ID через Guid.CreateVersion7

**Статус:** Принято

**Контекст:** Нужно связывать логи и трейсы в рамках одного запроса для отладки и мониторинга.

**Решение:** X-Correlation-Id генерируется через Guid.CreateVersion7 (.NET 10) или прокидывается из входящего заголовка. Добавляется в LogContext (Serilog) и Activity (OpenTelemetry).

**Последствия:**

- Time-ordered UUID (v7) — логи сортируются по времени
- Совместим с OTel trace-id
- Заголовок возвращается в ответе для клиента
- Не работает на health-checks (отдельный порт 8081)

---

## ADR-009: Result Pattern для бизнес-ошибок

**Статус:** Принято

**Контекст:** Требуется типобезопасная обработка ошибок без исключений для управления потоком. Exception Handler уже маппит необработанные исключения в JSON, но бизнес-ошибки (валидация, не найдено, конфликт, нарушение правила) нуждаются в отдельном, предсказуемом механизме.

**Рассмотренные альтернативы:**

1. **Только Exception Handler** — исключения для бизнес-ошибок. Минус: исключения дороги (стеко-трейс, аллокации), компилятор не контролирует обработку, тесты парсят текст сообщения.
2. **FluentResults** — сторонняя библиотека. Минус: внешняя зависимость, не контролируемый API, может конфликтовать с нашими расширениями.
3. **Собственная библиотека Result (inline)** — 9 файлов в `Contracts/Result/`, без зависимостей, полный контроль над контрактом. Минус: дублирование кода между проектами, необходимость поддерживать в каждом проекте.
4. **Fafp.ResultPattern (NuGet)** — собственная библиотека, вынесенная в переиспользуемый пакет. Публикуется на nuget.org, versioned, с SourceLink. Один источник правды для всех проектов экосистемы Firefly.

**Решение:** NuGet-пакет `Fafp.ResultPattern 1.0.0` (namespace `Faf.ResultPattern.*`). Инлайн-копия в `Contracts/Result/` удалена.

**Обоснование:**

- RFC 7807 ProblemDetails — стандартный формат для API, совместимый с OpenAPI/Scalar
- 5 типов ошибок покрывают 90% REST-сценариев: ValidationError (422), NotFoundError (404), ConflictError (409), ForbiddenError (403), BusinessRuleError (400)
- ValidationError с парсингом "field:code" → готовый протокол для фронтенда
- NuGet-пакет: один источник правды, versioning, SourceLink для отладки, обновления через `dotnet add package`
- `Match`, цепочки `OnSuccess`/`OnFailure` доступны из коробки

**Последствия:**

- Бизнес-ошибки — часть доменной модели, а не аварийные ситуации
- Контроллеры: `return result.ToActionResult()` вместо ручных `BadRequest`/`NotFound`
- Тестируемость: проверяем `error is NotFoundError`, а не парсим текст
- Exception Handler остаётся для непредвиденных технических сбоев (500, timeout)
- Обновления библиотеки — через NuGet, без правки кода в проекте

---

## ADR-010: OpenTelemetry для телеметрии

**Статус:** Принято

**Контекст:** Требуется полный observability: логи, трейсы, метрики. Нужен единый стек для сбора телеметрии, совместимый с промышленными системами мониторинга (Grafana, Prometheus, Jaeger).

**Рассмотренные альтернативы:**

1. **App Metrics** — старая библиотека, слабая поддержка, нет unified traces/metrics/logs.
2. **Prometheus .NET** — только метрики, нет трейсов и логов.
3. **OpenTelemetry** — стандарт CNCF, единый SDK для traces, metrics, logs, экспорт в OTLP.

**Решение:** OpenTelemetry.Extensions.Hosting с экспортом в OTLP (gRPC). Три сигнала: logs (OTLP), traces (OTLP + HttpClient instrumentation), metrics (OTLP + Runtime + HttpClient instrumentation). Конфигурация через appsettings.json, endpoint не меняется на лету (GitOps + restart).

**Обоснование:**

- CNCF стандарт — совместим с Grafana, Prometheus, Jaeger, Zipkin, Datadog
- Единый SDK для всех трёх сигналов (traces, metrics, logs)
- HttpClient instrumentation автоматически коррелирует outgoing запросы
- Runtime instrumentation даёт .NET метрики (GC, CPU, ThreadPool) из коробки
- OTLP протокол — стандарт для OpenTelemetry Collector

**Последствия:**

- Точка сбора: OTel Collector (отдельный контейнер/сервис)
- Логи дублируются: Serilog (application) + OTel (infra) — это осознанно
- Фильтры OTel-логов: Microsoft/System → Warning (уменьшает шум)
- Console exporter отключен даже в development (Serilog покрывает)
- Корреляция с Correlation ID через Activity.Current

**Связанные документы:**

- [`observability.md`](./observability.md) — настройки OTel, фильтры, endpoint

---

## ADR-011: Global Exception Handler Middleware

**Статус:** Принято

**Контекст:** Необработанные исключения в pipeline могут раскрыть внутренние детали (stack trace, IP, названия сервисов) клиенту и вызвать 500 без структурированного ответа. Нужен единый точка перехвата для безопасного формирования ошибок.

**Рассмотренные альтернативы:**

1. **try/catch в каждом контроллере** — дублирование кода, легко забыть, нет единообразия.
2. **IExceptionHandler** (.NET 8+) — новое API, но менее гибкое для нашего pipeline.
3. **Custom ExceptionHandlerMiddleware** — полный контроль над порядком middleware, маппингом, форматом.

**Решение:** `ExceptionHandlerMiddleware` — первый middleware в pipeline. Перехватывает все необработанные исключения, маппит на HTTP-коды и безопасный JSON `{"error": {"code": 400, "message": "..."}}`.

**Маппинг:**

| Исключение                  | HTTP-код | Сообщение                    |
| --------------------------- | -------- | ---------------------------- |
| ArgumentException           | 400      | Invalid request              |
| KeyNotFoundException        | 404      | Resource not found           |
| UnauthorizedAccessException | 403      | Access denied                |
| TimeoutException            | 504      | Request timed out            |
| Любое другое                | 500      | An unexpected error occurred |

**Обоснование:**

- Безопасность: stack trace, IP, пути файлов НЕ утекают наружу
- Единообразие: все ошибки в одном формате JSON
- Аудит: полная информация логируется на сервере через `LogError`
- Первый в pipeline: перехватывает исключения от всех последующих middleware

**Последствия:**

- Не перехватывает `/health/*` (отдельный pipeline на порту 8081)
- Не перехватывает `OperationCanceledException` (graceful shutdown)
- Сочетается с Result Pattern: бизнес-ошибки → Result, технические → Exception Handler

**Связанные документы:**

- [`operability.md`](./operability.md) — раздел Exception Handler

---

## ADR-012: Request/Response Logging Middleware

**Статус:** Принято

**Контекст:** Нужно логировать HTTP-запросы для отладки, аудита и мониторинга производительности. Тела запросов/ответов могут содержать чувствительные данные (пароли, токены), поэтому логируются только метаданные.

**Решение:** `RequestResponseLoggingMiddleware` логирует method, path, queryString, status code, duration. Не логирует тела request/response. Уровень логирования зависит от статуса: 2xx/3xx → Information, 4xx/5xx → Warning.

**Обоснование:**

- Минимальный оверхед: метаданные, не тела
- Безопасность: пароли, токены не попадают в логи
- Аудит: кто, когда, какой endpoint, какой статус
- Производительность: duration в миллисекундах для поиска медленных запросов

**Последствия:**

- Не логирует `/health/*` (отдельный pipeline)
- Формат: `HTTP GET /api/scenarios?page=1 → 200 (45ms)`
- Correlation ID добавляется автоматически через LogContext

**Связанные документы:**

- [`observability.md`](./observability.md) — раздел Request/Response Logging

---

## ADR-013: TUnit как тестовый фреймворк

**Статус:** Принято

**Контекст:** Нужен тестовый фреймворк для unit-тестов с поддержкой .NET 10, async/await, modern синтаксиса и интеграцией с покрытием кода.

**Рассмотренные альтернативы:**

1. **xUnit** — самый популярный, но синтаксис более шумный (Theory, InlineData, Assert.\*).
2. **NUnit** — классический, но устаревающий API ([Test], Assert.That без await).
3. **TUnit** — современный фреймворк, атрибуты как методы, нативный async/await, встроенный coverage.

**Решение:** TUnit 1.56.35 с Moq 4.20.72 для мокирования и Microsoft.Testing.Extensions.CodeCoverage 18.8.0 для покрытия.

**Обоснование:**

- Синтаксис: `[Test] public async Task Name()` — минимальный шум
- Assert: `await Assert.That(value).IsEqualTo(expected)` — нативный async
- Скорость: быстрее xUnit/NUnit на больших тестовых suite
- .NET 10: полная поддержка последнего runtime
- Coverage: встроенный сбор через `--coverage` параметр (Microsoft.Testing.Platform)

**Последствия:**

- HTML-отчёт через ReportGenerator
- Отдельный test runner (Microsoft.Testing.Platform, не VSTest)

---

## ADR-014: API Versioning (URL-based)

**Статус:** Принято

**Контекст:** API будет развиваться: новые поля, изменение контрактов, experimental endpoints. Нужен механизм совместимости с существующими клиентами при внесении изменений.

**Рассмотренные альтернативы:**

1. **Query string** (`?api-version=1.0`) — нестандартно, сложно кэшировать.
2. **Header** (`X-Api-Version: 1.0`) — чисто, но неудобно для отладки.
3. **URL path** (`/api/v1/...`) — стандарт, наглядно, удобно для маршрутизации.

**Решение:** `Asp.Versioning.Mvc` 10.0.0 с URL-based стратегией. Default version: 1.0. `ReportApiVersions: true`.

**Обоснование:**

- URL наглядный: `/api/v1/logging/level` — сразу понятно, какую версию используешь
- Cache-friendly: разные URL = разные ресурсы
- Стандарт для REST API
- Default version позволяет старым клиентам работать без изменений

**Последствия:**

- Все контроллеры получают `[ApiVersion("1.0")]` и `[Route("api/v{version:apiVersion}/[controller]")]`
- Заголовок `api-supported-versions` в ответе показывает доступные версии
- Новые версии добавляются через новые атрибуты на контроллерах
- Требуется миграция существующих маршрутов (`/api/logging/level` → `/api/v1/logging/level`)

**Связанные документы:**

- [`api.md`](./api.md) — эндпоинты с версиями
- [`architecture.md`](./architecture.md) — регистрация сервисов

---

## ADR-015: Scalar + Microsoft.AspNetCore.OpenApi для OpenAPI UI

**Статус:** Принято

**Контекст:** Нужен интерактивный UI для тестирования API в окружении Development с поддержкой JWT-авторизации. Требуется современное, быстрое решение, совместимое с .NET 10.

**Рассмотренные альтернативы:**

1. **Swashbuckle.AspNetCore** — популярная библиотека, но в версии 10.x возникли конфликты с Microsoft.OpenApi 3.x, проблемы с `AddSecurityRequirement`, `OperationFilter` не работал корректно.
2. **Scalar + Microsoft.AspNetCore.OpenApi** — официальное решение Microsoft для .NET 10, современный UI, нет конфликтов версий.

**Решение:** `Microsoft.AspNetCore.OpenApi` 10.0.9 + `Scalar.AspNetCore` 2.13.19.

**Конфигурация (единый источник метаданных):**

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Метаданные из ApiMetadataOptions — единый источник истины
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

if (app.Environment.IsDevelopment())
{
    var meta = app.Services.GetRequiredService<IOptions<ApiMetadataOptions>>().Value;

    app.MapOpenApi();  // /openapi/v1.json
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle(meta.Title)
            .WithTheme(ScalarTheme.Saturn);
    });  // /scalar/v1
}
```

**Обоснование:**

- Официальное решение Microsoft для .NET 10 — нет конфликтов версий
- Scalar UI — современный, быстрый, красивый (альтернатива Swagger UI)
- JWT Bearer схема встраивается через DocumentTransformer
- XML-комментарии автоматически включаются через source generator
- Работает только в Development (безопасность)
- **Единый источник метаданных** — те же `ApiMetadataOptions` используются в OpenAPI, Scalar UI и `/api/metadata` (см. ADR-016)

**Последствия:**

- Endpoints: `/openapi/v1.json` (OpenAPI 3.1 документ), `/scalar/v1` (UI)
- JWT-авторизация встроена в UI — кнопка Authorize
- XML-документация генерируется автоматически (`GenerateDocumentationFile = true`)
- Source generator `Microsoft.AspNetCore.OpenApi.SourceGenerators` включён по умолчанию
- Не влияет на Production (условие `IsDevelopment()`)

**Связанные документы:**

- [`api.md`](./api.md) — эндпоинты
- [`adr.md`](./adr.md) — ADR-016 (Metadata API)

---

## ADR-016: Публичный Metadata API Endpoint

**Статус:** Принято

**Контекст:** Нужен способ получения метаданных API (название, версия, описание, контакт разработчика) без аутентификации. Полезно для: автоматического документирования, мониторинга, интеграции с внешними системами, отображения версии API во фронтенде.

**Рассмотренные альтернативы:**

1. **OpenAPI JSON** (`/openapi/v1.json`) — содержит метаданные, но требует парсинга специфичного формата.
2. **Health check endpoint** — не подходит, т.к. health checks на отдельном порту.
3. **Отдельный `/api/metadata`** — простой JSON, читаемый любым клиентом.

**Решение:** Публичный endpoint `GET /api/metadata` с ответом в формате JSON.

**Конфигурация:**

- Значения берутся из `ApiMetadataOptions` (appsettings.json) — единый источник с OpenAPI (ADR-015)
- Не требует аутентификации (`.AllowAnonymous()`)
- Кэширование: 1 час (`ResponseCache(Duration = 3600)`)
- Endpoint виден в OpenAPI-документации

**Обоснование:**

- Публичные метаданные не являются секретом
- Простой JSON без парсинга OpenAPI-схемы
- Кэширование снижает нагрузку
- Отдельный от health checks — разная семантика (метаданные vs состояние)
- **Единый источник истины** — бэкенд (OpenAPI, Scalar) и фронтенд читают одни и те же данные из `appsettings.json`
- Изменение конфигурации — все клиенты получают обновлённые метаданные (с задержкой кэша 1 час)

**Последствия:**

- Endpoint доступен без аутентификации
- Значения конфигурируются через `appsettings.json`
- Автоматически отражают текущую конфигурацию API
- Фронтенд может запросить `/api/metadata` и отобразить название/версию API в UI
- При изменении конфигурации — требуется restart приложения (т.к. используется `IOptions<T>`, а не `IOptionsMonitor<T>`)

**Связанные документы:**

- [`api.md`](./api.md) — секция Metadata API
- [`operability.md`](./operability.md) — раздел Response Caching
- [`adr.md`](./adr.md) — ADR-015 (Scalar + OpenAPI)

---
