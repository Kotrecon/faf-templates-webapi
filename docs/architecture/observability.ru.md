# Observability — Текущее состояние

| Поле       | Значение                    |
| ---------- | --------------------------- |
| **Версия** | SERVICE_VERSION_PLACEHOLDER |
| **Статус** | Active                      |
| **Дата**   | 2026-07-08                  |

---

## Логирование (Serilog)

| Параметр     | Production                                           | Development                                          |
| ------------ | ---------------------------------------------------- | ---------------------------------------------------- |
| Root level   | Information                                          | Debug                                                |
| Microsoft    | Warning                                              | Information                                          |
| System       | Warning                                              | Information                                          |
| Console sink | false                                                | true (текстовый формат)                              |
| File sink    | true (JSON, rolling day, 14 дней)                    | true (текстовый формат, 7 дней)                      |
| Enrichers    | FromLogContext, WithMachineName, WithEnvironmentName | FromLogContext, WithMachineName, WithEnvironmentName |

**Файлы логов:**

- Production: `logs/WebApiTemplate.log` (RollingInterval: Day, retention: 14 дней)
- Development: `logs/dev-.log` (RollingInterval: Day, retention: 7 дней)

**Динамическое управление:**

- `LoggingLevelSwitch` зарегистрирован в DI (singleton).
- Root level и overrides менять на лету через `PUT /api/v1/logging/level`.
- Fatal и Verbose запрещены (безопасность).

---

## Request/Response Logging Middleware

### Концепция

Middleware логирует входящие запросы и исходящие ответы для observability, аудита и отладки. Работает поверх Serilog.

### Что логирует

- HTTP-метод (GET, POST, PUT, DELETE).
- Path + QueryString.
- Status Code.
- Duration (мс).

### Формат лога

```bash
[Information] HTTP GET /api/v1/scenarios?page=1 → 200 (45ms)
[Warning]     HTTP POST /api/v1/scenarios → 400 (12ms)
```

### Уровни логирования

| Статус ответа | Уровень     |
| ------------- | ----------- |
| 2xx, 3xx      | Information |
| 4xx, 5xx      | Warning     |

### Исключения

- Health checks на порту 8081 — **НЕ логируются** (отдельный pipeline).
- Metadata API (`/api/metadata`) — логируется как обычный запрос.

### Файлы

- `Extensions/RequestResponseLogging/RequestResponseLoggingMiddleware.cs`
- `Extensions/RequestResponseLogging/RequestResponseLoggingExtensions.cs`

### Тесты (2)

- `InvokeAsync_LogsSuccessfulRequest`
- `InvokeAsync_LogsErrorStatusAsWarning`

---

## Correlation ID Middleware

### Концепция

Middleware генерирует уникальный идентификатор запроса (Correlation ID) или принимает его от клиента, и прокидывает его через весь pipeline: `HttpContext.Items`, заголовок ответа, `Activity` (distributed tracing) и Serilog `LogContext`.

### Что делает

- Получает `X-Correlation-Id` из входящего заголовка (если есть).
- Генерирует новый `Guid.CreateVersion7()` (time-ordered, .NET 10) — если заголовок отсутствует или пуст.
- Сохраняет значение в `context.Items["CorrelationId"]` — доступно контроллерам и другим middleware.
- Устанавливает тег `correlation.id` в `Activity.Current` — интеграция с OpenTelemetry distributed tracing.
- Обогащает все Serilog-логи в рамках запроса через `LogContext.PushProperty("CorrelationId", ...)` — автоматически попадает в каждую лог-запись.
- Возвращает `X-Correlation-Id` в заголовке ответа — клиент может использовать его для корреляции.

### Формат лога (с Correlation ID)

```bash
[Information] [CorrelationId: 01923abc-...] HTTP GET /api/v1/scenarios → 200 (45ms)
```

### Исключения

- Health checks на порту 8081 — **НЕ обрабатываются** (отдельный pipeline, Correlation ID не видит запросов `/health/*`).
- Metadata API (`/api/metadata`) — обрабатывается, Correlation ID добавляется.

### Файлы

- `Extensions/CorrelationId/CorrelationIdMiddleware.cs`
- `Extensions/CorrelationId/CorrelationIdExtensions.cs`

### Тесты (4)

- `InvokeAsync_WhenHeaderMissing_GeneratesCorrelationId` — без заголовка генерируется валидный GUID.
- `InvokeAsync_WhenHeaderPresent_UsesIncomingCorrelationId` — входящий заголовок проходит через.
- `InvokeAsync_SetsCorrelationIdInItems` — значение доступно в `HttpContext.Items`.
- `InvokeAsync_SetsActivityTag` — тег `correlation.id` установлен в `Activity`.

---

## Логирование бизнес-ошибок (Result Pattern)

### Концепция

При использовании Result Pattern бизнес-ошибки логируются как структурированные данные через Serilog `LogContext`. Каждая ошибка обогащает лог тремя полями:

- `ErrorCode` — машинно-читаемый код (для фильтрации и алертов)
- `ErrorMessage` — человекочитаемое описание
- `StatusCode` — HTTP-статус для категоризации

### Формат лога ошибки

```bash
[Warning] [CorrelationId: 01923abc-...] [ErrorCode: ValidationFailed]
[StatusCode: 422] Operation failed: Данные не прошли валидацию
```

### Интеграция с Serilog

```csharp
using (LogContext.PushProperty("CorrelationId", correlationId))
using (LogContext.PushProperty("ErrorCode", error.Code))
using (LogContext.PushProperty("StatusCode", error.StatusCode))
{
    _logger.Warning("Operation failed: {ErrorMessage} ({StatusCode})",
        error.Message, error.StatusCode);
}
```

### Связь с ProblemDetails

Формат ProblemDetails (RFC 7807) используется как **единый стандарт** для HTTP-ответов И логов ошибок — клиенты и мониторинг говорят на одном языке.

Детали реализации Result Pattern — в [`result-pattern.md`](./result-pattern.md).

---

## Телеметрия (OpenTelemetry)

| Параметр         | Production                                  | Development                                 |
| ---------------- | ------------------------------------------- | ------------------------------------------- |
| Endpoint         | `http://otel-collector:4317`                | `http://localhost:4317`                     |
| Protocol         | gRPC                                        | gRPC                                        |
| Console exporter | false                                       | false                                       |
| Logs             | OTLP                                        | OTLP                                        |
| Traces           | OTLP + HttpClient instrumentation           | OTLP + HttpClient instrumentation           |
| Metrics          | OTLP + Runtime instrumentation + HttpClient | OTLP + Runtime instrumentation + HttpClient |

**Фильтры OTel-логов (отдельно от Serilog):**

- Default: Information
- Microsoft: Warning
- System: Warning

**Конфигурация:**

- `OpenTelemetryOptions` в `appsettings.json`
- Регистрация через `ObservabilityExtensions.AddCustomOpenTelemetry()`
- Валидация при старте через `ValidateOnStart()`

---

## Контроллеры

| Контроллер        | Роуты                            | Доступ      |
| ----------------- | -------------------------------- | ----------- |
| LoggingController | `GET /api/v1/logging/level`      | AuditViewer |
|                   | `PUT /api/v1/logging/level`      | AdminOnly   |
|                   | `GET /api/v1/logging/categories` | AuditViewer |

---
