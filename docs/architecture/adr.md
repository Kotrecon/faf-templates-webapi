# ADR — Architecture Decision Records

| Field       | Value                       |
| ----------- | --------------------------- |
| **Version** | SERVICE_VERSION_PLACEHOLDER |
| **Status**  | Active                      |
| **Date**    | 2026-07-08                  |

> Architectural decisions made in the project. Each decision captures context, decision, and consequences.

---

## ADR-001: Web SDK instead of Console SDK

**Status:** Accepted

**Context:** The project started as a console app (Microsoft.NET.Sdk), but ASP.NET Core was needed for API, controllers, JWT auth.

**Decision:** Switch to `Microsoft.NET.Sdk.Web`.

**Consequences:**

- AddControllers, MapControllers, middleware pipeline available
- Kestrel built-in
- No additional packages needed (`Microsoft.AspNetCore.*` already in SDK)

---

## ADR-002: Serilog via appsettings.json

**Status:** Accepted

**Context:** Serilog can be configured programmatically (WriteTo.Console) or via configuration (ReadFrom.Configuration).

**Decision:** Standard Serilog format in appsettings.json + ReadFrom.Configuration.

**Consequences:**

- Sinks configured via JSON (not code)
- Add/remove sinks without recompilation
- Configuration-based enrichers

---

## ADR-003: LoggingLevelSwitch in DI

**Status:** Accepted

**Context:** Log levels must be changed at runtime via API without restart.

**Decision:** LoggingLevelSwitch registered as singleton in DI, controller receives it via constructor injection.

**Consequences:**

- Dynamic level changes via PUT /api/logging/level
- No restart to change level
- Fatal and Verbose forbidden (security)

---

## ADR-004: Policy-based authorization

**Status:** Accepted

**Context:** Need to differentiate access: Admin (write), Operator (read), Auditor (read-only).

**Decision:** Policy-based authorization with three policies: AdminOnly, Operator, AuditViewer.

**Consequences:**

- Flexibility: add policies without changing controller code
- Testability: policies verifiable via IAuthorizationService
- Readability: `[Authorize(Policy = "AdminOnly")]` instead of `[Authorize(Roles = "Admin")]`
- **Important:** See [`auth-flow.md`](./auth-flow.md) — `[Authorize]` on class + method = AND logic. Class attribute requires passing for each method

---

## ADR-005: Health checks on separate port

**Status:** Accepted

**Context:** Health checks must be available to orchestrators (Kubelet, Prometheus) but not externally accessible.

**Decision:** Two Kestrel endpoints: API (8080) and Health (8081). Health port not routed externally (firewall / Nginx / Cloud LB).

**Consequences:**

- Middleware (logging, correlation, Scalar, CORS) automatically excluded from health
- Network isolation instead of secret headers
- Easier firewall / Nginx configuration

---

## ADR-006: Rate limiting on health endpoints

**Status:** Accepted

**Context:** Even with network isolation, credential leaks or DDoS attacks can overload DB via health checks.

**Decision:** Fixed window limiter: 30 requests per 10 seconds on `/health/*`.

**Consequences:**

- Additional protection layer (not primary)
- Kubelet (5-10 sec) + Prometheus (15-30 sec) + 3 endpoints = ~5-6 requests per 10 sec
- Limit 30 — with margin

---

## ADR-007: CORS AllowAll for development

**Status:** Accepted (temporary)

**Context:** Frontend and API may run on different ports/domains during development. Need to allow cross-origin requests.

**Decision:** AllowAll policy (`*` origins, `*` methods, `*` headers) for development. Will be restricted in production.

**Consequences:**

- Convenient for local development
- Not secure for production (need to restrict origins)
- Configuration in appsettings.json, policy in CorsExtensions.cs

---

## ADR-008: Correlation ID via Guid.CreateVersion7

**Status:** Accepted

**Context:** Need to correlate logs and traces within a single request for debugging and monitoring.

**Decision:** X-Correlation-Id generated via Guid.CreateVersion7 (.NET 10) or forwarded from incoming header. Added to LogContext (Serilog) and Activity (OpenTelemetry).

**Consequences:**

- Time-ordered UUID (v7) — logs sorted by time
- Compatible with OTel trace-id
- Header returned in response for client
- Not applied to health checks (separate port 8081)

---

## ADR-009: Result Pattern for business errors

**Status:** Accepted

**Context:** Type-safe error handling without exceptions is needed for flow control. Exception Handler already maps unhandled exceptions to JSON, but business errors (validation, not found, conflict, rule violation) need a separate, predictable mechanism.

**Alternatives considered:**

1. **Exception Handler only** — exceptions for business errors. Downside: exceptions are expensive (stack trace, allocations), compiler doesn't enforce handling, tests parse message text.
2. **FluentResults** — third-party library. Downside: external dependency, uncontrolled API, may conflict with our extensions.
3. **Custom Result library (inline)** — 9 files in `Contracts/Result/`, no dependencies, full contract control. Downside: code duplication across projects, maintenance burden in each project.
4. **Fafp.ResultPattern (NuGet)** — own library packaged as reusable NuGet. Published on nuget.org, versioned, with SourceLink. Single source of truth for all Firefly ecosystem projects.

**Decision:** NuGet package `Fafp.ResultPattern 1.0.0` (namespace `Faf.ResultPattern.*`). Inline copy in `Contracts/Result/` removed.

**Rationale:**

- RFC 7807 ProblemDetails — standard API format, compatible with OpenAPI/Scalar
- 5 error types cover 90% of REST scenarios: ValidationError (422), NotFoundError (404), ConflictError (409), ForbiddenError (403), BusinessRuleError (400)
- ValidationError with "field:code" parsing → ready protocol for frontend
- NuGet package: single source of truth, versioning, SourceLink for debugging, updates via `dotnet add package`
- `Match`, `OnSuccess`/`OnFailure` chains available out of the box

**Consequences:**

- Business errors are part of the domain model, not exceptional situations
- Controllers: `return result.ToActionResult()` instead of manual `BadRequest`/`NotFound`
- Testability: check `error is NotFoundError` instead of parsing text
- Exception Handler remains for unforeseen technical failures (500, timeout)
- Library updates via NuGet, no code changes in project

---

## ADR-010: OpenTelemetry for observability

**Status:** Accepted

**Context:** Full observability required: logs, traces, metrics. Need a unified telemetry collection stack compatible with industry monitoring systems (Grafana, Prometheus, Jaeger).

**Alternatives considered:**

1. **App Metrics** — legacy library, weak support, no unified traces/metrics/logs.
2. **Prometheus .NET** — metrics only, no traces or logs.
3. **OpenTelemetry** — CNCF standard, unified SDK for traces, metrics, logs, OTLP export.

**Decision:** OpenTelemetry.Extensions.Hosting with OTLP export (gRPC). Three signals: logs (OTLP), traces (OTLP + HttpClient instrumentation), metrics (OTLP + Runtime + HttpClient instrumentation). Configuration via appsettings.json, endpoint not changeable at runtime (GitOps + restart).

**Rationale:**

- CNCF standard — compatible with Grafana, Prometheus, Jaeger, Zipkin, Datadog
- Unified SDK for all three signals (traces, metrics, logs)
- HttpClient instrumentation automatically correlates outgoing requests
- Runtime instrumentation provides .NET metrics (GC, CPU, ThreadPool) out of the box
- OTLP protocol — standard for OpenTelemetry Collector

**Consequences:**

- Collection point: OTel Collector (separate container/service)
- Logs duplicated: Serilog (application) + OTel (infra) — intentional
- OTel log filters: Microsoft/System → Warning (reduces noise)
- Console exporter disabled even in development (Serilog covers)
- Correlation with Correlation ID via Activity.Current

**Related documents:**

- [`observability.md`](./observability.md) — OTel configuration, filters, endpoint

---

## ADR-011: Global Exception Handler Middleware

**Status:** Accepted

**Context:** Unhandled exceptions in pipeline may expose internal details (stack trace, IP, service names) to the client and cause 500 without structured response. Need a single interception point for safe error formatting.

**Alternatives considered:**

1. **try/catch in each controller** — code duplication, easy to forget, no consistency.
2. **IExceptionHandler** (.NET 8+) — new API, but less flexible for our pipeline.
3. **Custom ExceptionHandlerMiddleware** — full control over middleware order, mapping, format.

**Decision:** `ExceptionHandlerMiddleware` — first middleware in pipeline. Catches all unhandled exceptions, maps to HTTP codes and safe JSON `{"error": {"code": 400, "message": "..."}}`.

**Mapping:**

| Exception                   | HTTP Code | Message                      |
| --------------------------- | --------- | ---------------------------- |
| ArgumentException           | 400       | Invalid request              |
| KeyNotFoundException        | 404       | Resource not found           |
| UnauthorizedAccessException | 403       | Access denied                |
| TimeoutException            | 504       | Request timed out            |
| Any other                   | 500       | An unexpected error occurred |

**Rationale:**

- Security: stack trace, IP, file paths NOT leaked externally
- Consistency: all errors in one JSON format
- Audit: full information logged on server via `LogError`
- First in pipeline: catches exceptions from all subsequent middleware

**Consequences:**

- Does not catch `/health/*` (separate pipeline on port 8081)
- Does not catch `OperationCanceledException` (graceful shutdown)
- Complements Result Pattern: business errors → Result, technical errors → Exception Handler

**Related documents:**

- [`operability.md`](./operability.md) — Exception Handler section

---

## ADR-012: Request/Response Logging Middleware

**Status:** Accepted

**Context:** HTTP requests need logging for debugging, auditing, and performance monitoring. Request/response bodies may contain sensitive data (passwords, tokens), so only metadata is logged.

**Decision:** `RequestResponseLoggingMiddleware` logs method, path, queryString, status code, duration. Does not log request/response bodies. Log level depends on status: 2xx/3xx → Information, 4xx/5xx → Warning.

**Rationale:**

- Minimal overhead: metadata, not bodies
- Security: passwords, tokens don't reach logs
- Audit: who, when, which endpoint, which status
- Performance: duration in milliseconds for finding slow requests

**Consequences:**

- Does not log `/health/*` (separate pipeline)
- Format: `HTTP GET /api/scenarios?page=1 → 200 (45ms)`
- Correlation ID added automatically via LogContext

**Related documents:**

- [`observability.md`](./observability.md) — Request/Response Logging section

---

## ADR-013: TUnit as test framework

**Status:** Accepted

**Context:** Need a test framework for unit tests with .NET 10 support, async/await, modern syntax, and code coverage integration.

**Alternatives considered:**

1. **xUnit** — most popular, but noisier syntax (Theory, InlineData, Assert.\*).
2. **NUnit** — classic, but aging API ([Test], Assert.That without await).
3. **TUnit** — modern framework, attributes as methods, native async/await, built-in coverage.

**Decision:** TUnit 1.56.35 with Moq 4.20.72 for mocking and Microsoft.Testing.Extensions.CodeCoverage 18.8.0 for coverage.

**Rationale:**

- Syntax: `[Test] public async Task Name()` — minimal noise
- Assert: `await Assert.That(value).IsEqualTo(expected)` — native async
- Speed: faster than xUnit/NUnit on large test suites
- .NET 10: full support for latest runtime
- Coverage: built-in collection via `--coverage` parameter (Microsoft.Testing.Platform)

**Consequences:**

- HTML report via ReportGenerator
- Separate test runner (Microsoft.Testing.Platform, not VSTest)

---

## ADR-014: API Versioning (URL-based)

**Status:** Accepted

**Context:** API will evolve: new fields, contract changes, experimental endpoints. Need a mechanism for backward compatibility with existing clients when making changes.

**Alternatives considered:**

1. **Query string** (`?api-version=1.0`) — non-standard, hard to cache.
2. **Header** (`X-Api-Version: 1.0`) — clean, but inconvenient for debugging.
3. **URL path** (`/api/v1/...`) — standard, visual, convenient for routing.

**Decision:** `Asp.Versioning.Mvc` 10.0.0 with URL-based strategy. Default version: 1.0. `ReportApiVersions: true`.

**Rationale:**

- URL is visual: `/api/v1/logging/level` — immediately clear which version is used
- Cache-friendly: different URLs = different resources
- Standard for REST API
- Default version allows old clients to work without changes

**Consequences:**

- All controllers get `[ApiVersion("1.0")]` and `[Route("api/v{version:apiVersion}/[controller]")]`
- `api-supported-versions` header in response shows available versions
- New versions added via new attributes on controllers
- Existing routes need migration (`/api/logging/level` → `/api/v1/logging/level`)

**Related documents:**

- [`api.md`](./api.md) — endpoints with versions
- [`architecture.md`](./architecture.md) — service registration

---

## ADR-015: Scalar + Microsoft.AspNetCore.OpenApi for OpenAPI UI

**Status:** Accepted

**Context:** Need an interactive UI for API testing in Development environment with JWT authorization support. Requires a modern, fast solution compatible with .NET 10.

**Alternatives considered:**

1. **Swashbuckle.AspNetCore** — popular library, but version 10.x had conflicts with Microsoft.OpenApi 3.x, `AddSecurityRequirement` issues, `OperationFilter` not working correctly.
2. **Scalar + Microsoft.AspNetCore.OpenApi** — official Microsoft solution for .NET 10, modern UI, no version conflicts.

**Decision:** `Microsoft.AspNetCore.OpenApi` 10.0.9 + `Scalar.AspNetCore` 2.13.19.

**Configuration (single source of metadata):**

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Metadata from ApiMetadataOptions — single source of truth
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
                Description = "JWT token for authorization"
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

**Rationale:**

- Official Microsoft solution for .NET 10 — no version conflicts
- Scalar UI — modern, fast, beautiful (alternative to Swagger UI)
- JWT Bearer scheme embedded via DocumentTransformer
- XML comments automatically included via source generator
- Works only in Development (security)
- **Single source of metadata** — same `ApiMetadataOptions` used in OpenAPI, Scalar UI, and `/api/metadata` (see ADR-016)

**Consequences:**

- Endpoints: `/openapi/v1.json` (OpenAPI 3.1 document), `/scalar/v1` (UI)
- JWT authorization built into UI — Authorize button
- XML documentation generated automatically (`GenerateDocumentationFile = true`)
- Source generator `Microsoft.AspNetCore.OpenApi.SourceGenerators` enabled by default
- Does not affect Production (condition `IsDevelopment()`)

**Related documents:**

- [`api.md`](./api.md) — endpoints
- [`adr.md`](./adr.md) — ADR-016 (Metadata API)

---

## ADR-016: Public Metadata API Endpoint

**Status:** Accepted

**Context:** Need a way to retrieve API metadata (title, version, description, developer contact) without authentication. Useful for: automatic documentation, monitoring, external system integration, displaying API version in frontend.

**Alternatives considered:**

1. **OpenAPI JSON** (`/openapi/v1.json`) — contains metadata, but requires parsing a specific format.
2. **Health check endpoint** — not suitable, as health checks are on a separate port.
3. **Dedicated `/api/metadata`** — simple JSON, readable by any client.

**Decision:** Public endpoint `GET /api/metadata` with JSON response.

**Configuration:**

- Values from `ApiMetadataOptions` (appsettings.json) — same source as OpenAPI (ADR-015)
- No authentication required (`.AllowAnonymous()`)
- Caching: 1 hour (`ResponseCache(Duration = 3600)`)
- Endpoint visible in OpenAPI documentation

**Rationale:**

- Public metadata is not a secret
- Simple JSON without OpenAPI schema parsing
- Caching reduces load
- Separate from health checks — different semantics (metadata vs status)
- **Single source of truth** — backend (OpenAPI, Scalar) and frontend read the same data from `appsettings.json`
- Configuration change — all clients receive updated metadata (with cache delay of 1 hour)

**Consequences:**

- Endpoint accessible without authentication
- Values configured via `appsettings.json`
- Automatically reflect current API configuration
- Frontend can request `/api/metadata` and display API title/version in UI
- Configuration change requires application restart (uses `IOptions<T>`, not `IOptionsMonitor<T>`)

**Related documents:**

- [`api.md`](./api.md) — Metadata API section
- [`operability.md`](./operability.md) — Response Caching section
- [`adr.md`](./adr.md) — ADR-015 (Scalar + OpenAPI)

---
