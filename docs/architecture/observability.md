# Observability — Current State

| Field       | Value                       |
| ----------- | --------------------------- |
| **Version** | SERVICE_VERSION_PLACEHOLDER |
| **Status**  | Active                      |
| **Date**    | 2026-07-08                  |

---

## Logging (Serilog)

| Parameter     | Production                                           | Development                                          |
| ------------- | ---------------------------------------------------- | ---------------------------------------------------- |
| Root level    | Information                                          | Debug                                                |
| Microsoft     | Warning                                              | Information                                          |
| System        | Warning                                              | Information                                          |
| Console sink  | false                                                | true (text format)                                   |
| File sink     | true (JSON, rolling day, 14 days)                    | true (text format, 7 days)                           |
| Enrichers     | FromLogContext, WithMachineName, WithEnvironmentName | FromLogContext, WithMachineName, WithEnvironmentName |

**Log files:**

- Production: `logs/WebApiTemplate.log` (RollingInterval: Day, retention: 14 days)
- Development: `logs/dev-.log` (RollingInterval: Day, retention: 7 days)

**Dynamic management:**

- `LoggingLevelSwitch` is registered in DI (singleton).
- Root level and overrides can be changed at runtime via `PUT /api/v1/logging/level`.
- Fatal and Verbose are disabled (security).

---

## Request/Response Logging Middleware

### Concept

The middleware logs incoming requests and outgoing responses for observability, auditing, and debugging. It works on top of Serilog.

### What it logs

- HTTP method (GET, POST, PUT, DELETE).
- Path + QueryString.
- Status Code.
- Duration (ms).

### Log format

```bash
[Information] HTTP GET /api/v1/scenarios?page=1 → 200 (45ms)
[Warning]     HTTP POST /api/v1/scenarios → 400 (12ms)
```

### Log levels

| Response Status | Level       |
| --------------- | ----------- |
| 2xx, 3xx        | Information |
| 4xx, 5xx        | Warning     |

### Exceptions

- Health checks on port 8081 are **NOT logged** (separate pipeline).
- Metadata API (`/api/metadata`) — logged as a regular request.

### Files

- `Extensions/RequestResponseLogging/RequestResponseLoggingMiddleware.cs`
- `Extensions/RequestResponseLogging/RequestResponseLoggingExtensions.cs`

### Tests (2)

- `InvokeAsync_LogsSuccessfulRequest`
- `InvokeAsync_LogsErrorStatusAsWarning`

---

## Correlation ID Middleware

### Concept

The middleware generates a unique request identifier (Correlation ID) or accepts it from the client, and propagates it through the entire pipeline: `HttpContext.Items`, response header, `Activity` (distributed tracing), and Serilog `LogContext`.

### What it does

- Retrieves `X-Correlation-Id` from the incoming header (if present).
- Generates a new `Guid.CreateVersion7()` (time-ordered, .NET 10) — if the header is missing or empty.
- Stores the value in `context.Items["CorrelationId"]` — accessible to controllers and other middleware.
- Sets the `correlation.id` tag in `Activity.Current` — integration with OpenTelemetry distributed tracing.
- Enriches all Serilog logs within the request via `LogContext.PushProperty("CorrelationId", ...)` — automatically included in every log entry.
- Returns `X-Correlation-Id` in the response header — the client can use it for correlation.

### Log format (with Correlation ID)

```bash
[Information] [CorrelationId: 01923abc-...] HTTP GET /api/v1/scenarios → 200 (45ms)
```

### Exceptions

- Health checks on port 8081 are **NOT processed** (separate pipeline, Correlation ID does not see `/health/*` requests).
- Metadata API (`/api/metadata`) — processed, Correlation ID is added.

### Files

- `Extensions/CorrelationId/CorrelationIdMiddleware.cs`
- `Extensions/CorrelationId/CorrelationIdExtensions.cs`

### Tests (4)

- `InvokeAsync_WhenHeaderMissing_GeneratesCorrelationId` — without a header, a valid GUID is generated.
- `InvokeAsync_WhenHeaderPresent_UsesIncomingCorrelationId` — the incoming header is passed through.
- `InvokeAsync_SetsCorrelationIdInItems` — the value is available in `HttpContext.Items`.
- `InvokeAsync_SetsActivityTag` — the `correlation.id` tag is set in `Activity`.

---

## Business Error Logging (Result Pattern)

### Concept

When using the Result Pattern, business errors are logged as structured data through Serilog `LogContext`. Each error enriches the log with three fields:

- `ErrorCode` — machine-readable code (for filtering and alerts)
- `ErrorMessage` — human-readable description
- `StatusCode` — HTTP status for categorization

### Error log format

```bash
[Warning] [CorrelationId: 01923abc-...] [ErrorCode: ValidationFailed]
[StatusCode: 422] Operation failed: Data validation failed
```

### Integration with Serilog

```csharp
using (LogContext.PushProperty("CorrelationId", correlationId))
using (LogContext.PushProperty("ErrorCode", error.Code))
using (LogContext.PushProperty("StatusCode", error.StatusCode))
{
    _logger.Warning("Operation failed: {ErrorMessage} ({StatusCode})",
        error.Message, error.StatusCode);
}
```

### Connection to ProblemDetails

The ProblemDetails format (RFC 7807) is used as a **unified standard** for HTTP responses AND error logs — clients and monitoring speak the same language.

Details of the Result Pattern implementation — in [`result-pattern.md`](./result-pattern.md).

---

## Telemetry (OpenTelemetry)

| Parameter       | Production                                  | Development                                 |
| --------------- | ------------------------------------------- | ------------------------------------------- |
| Endpoint        | `http://otel-collector:4317`                | `http://localhost:4317`                     |
| Protocol        | gRPC                                        | gRPC                                        |
| Console exporter| false                                       | false                                       |
| Logs            | OTLP                                        | OTLP                                        |
| Traces          | OTLP + HttpClient instrumentation           | OTLP + HttpClient instrumentation           |
| Metrics         | OTLP + Runtime instrumentation + HttpClient | OTLP + Runtime instrumentation + HttpClient |

**OTel log filters (separate from Serilog):**

- Default: Information
- Microsoft: Warning
- System: Warning

**Configuration:**

- `OpenTelemetryOptions` in `appsettings.json`
- Registration via `ObservabilityExtensions.AddCustomOpenTelemetry()`
- Validation on startup via `ValidateOnStart()`

---

## Controllers

| Controller        | Routes                           | Access      |
| ----------------- | -------------------------------- | ----------- |
| LoggingController | `GET /api/v1/logging/level`      | AuditViewer |
|                   | `PUT /api/v1/logging/level`      | AdminOnly   |
|                   | `GET /api/v1/logging/categories` | AuditViewer |

---
