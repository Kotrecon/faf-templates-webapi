# Operability — WebApiTemplate Service Production Readiness

| Field       | Value                       |
| ----------- | --------------------------- |
| **Version** | SERVICE_VERSION_PLACEHOLDER |
| **Status**  | Active                      |
| **Date**    | 2026-07-08                  |

> This document describes how the service behaves in production: how it is monitored, how it restarts, how it handles overload protection, and how it processes errors. Intended for developers, DevOps, and SRE.

---

## 🩺 Health Checks

### What They Are

HTTP endpoints that the orchestrator (Kubernetes, Docker) polls regularly to determine the service's state. There are three types:

- **Liveness** (`/health/live`) — "Is the process alive?" If not → the container restarts.
- **Readiness** (`/health/ready`) — "Ready to accept traffic?" If not → traffic is routed to other pods, but the pod is NOT restarted.
- **Startup** (`/health`) — "Has initialization completed?" Used during slow startup (cache warming, migrations).

**Key rule:** liveness must be as lightweight as possible (no DB/Redis), otherwise a dependency failure will trigger cascading restarts of all pods.

### Health Endpoint Comparison

| Characteristic                 | `/health/live` (Liveness)                                                                                                                                                            | `/health/ready` (Readiness)                                                                                                                                                                       | `/health` (Base)                                                                                                                                                                      |
| :----------------------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Main question**              | Is the application process working or has it hung/died?                                                                                                                              | Is the service ready to accept and process user traffic right now?                                                                                                                                | What is the overall state of the service and all its dependencies?                                                                                                                    |
| **What it checks**             | Only that the HTTP server is alive and can accept requests. No DB, Redis, or external API checks. Checks the event loop, absence of deadlocks, and memory leaks.                     | Active connections to critical dependencies: DB (connection pool check), Redis/Kafka, completion of startup procedures (cache warming, config loading, service mesh initialization).              | Aggregates Liveness and Readiness checks, returns detailed status of each dependency (DB, cache, queues, external APIs), service version, uptime, resource usage metrics.             |
| **HTTP response codes**        | `200 OK` — process is alive<br>`503 Service Unavailable` — process has hung                                                                                                          | `200 OK` — ready to accept traffic<br>`503 Service Unavailable` — dependencies unavailable, not ready                                                                                             | `200 OK` — all systems operational<br>`503 Service Unavailable` — critical dependencies are down                                                                                      |
| **Who polls it**               | Kubelet (Kubernetes), Docker healthcheck, systemd watchdog                                                                                                                           | Kube-proxy, Ingress Controller, Service Mesh (Istio, Linkerd), internal Load Balancers (AWS ALB, Yandex LB)                                                                                       | Prometheus (blackbox exporter), Zabbix, UptimeRobot, Grafana dashboards, admin monitoring panels                                                                                      |
| **Reaction to 503 / Timeout**  | **Restart:** Orchestrator kills the container (kill -9) and restarts it. Restart counter increments. When the limit is exceeded (CrashLoopBackOff) — pod transitions to Error state. | **Remove:** Pod is removed from endpoints/balancer pool. Traffic is redirected to other healthy pods. Pod is NOT restarted. When the dependency recovers — pod automatically returns to the pool. | **Alert:** Monitoring records the incident, displays a red status on the dashboard, sends notification to the on-call engineer via Slack/PagerDuty/Telegram. Does not affect routing. |
| **Response time requirements** | Must respond within **< 100ms**. Any delay = risk of false restart.                                                                                                                  | Can respond slower — **up to 1-3 seconds**. Delays are acceptable during connection pool checks.                                                                                                  | Not critical — **up to 5-10 seconds**. Used for monitoring, does not affect routing.                                                                                                  |
| **Poll frequency**             | Every **5-10 seconds** (configurable via `periodSeconds`)                                                                                                                            | Every **5-10 seconds** (configurable via `periodSeconds`)                                                                                                                                         | Every **30-60 seconds** or on demand (Prometheus pull model)                                                                                                                          |
| **Timeout**                    | Typically **1-2 seconds**. If no response within this time — considered failed.                                                                                                      | Typically **3-5 seconds**. Time is given for dependency checks.                                                                                                                                   | Typically **10-30 seconds**. Long-running checks are acceptable.                                                                                                                      |
| **Misconfiguration risks**     | If DB is checked here — when DB goes down Kubernetes will endlessly restart pods (Cascading Failure).                                                                                | If made too heavy (JOINs, complex queries) — the healthcheck itself will start stressing the DB and may bring it down.                                                                            | If details (DB IP, stack traces) are exposed to the public network — information leak for attackers.                                                                                  |
| **Best Practices**             | - No external dependencies<br>- Minimal logic<br>- Network isolation via separate port (8081)                                                                                        | - Check only critical dependencies<br>- Use `SELECT 1` instead of heavy queries<br>- Cache results for 5 seconds<br>- Rate limiting                                                               | - Sanitize responses (don't expose internals)<br>- Port blocked from external network at infrastructure level<br>- Return only necessary information                                  |

### How It's Implemented Here

**Architectural decision:** health checks are on a separate port `8081`, API runs on `8080`. This provides automatic isolation from middleware (logging, correlation ID, CORS, exception handler, Scalar) — they simply don't see health requests.

### Current State

**Ports:**

- Two Kestrel endpoints: API (8080) and Health (8081) — `appsettings.json`.

**Endpoints:**

- `/health/live` — delegate, no separate class, no dependencies.
- `/health/ready` — `ReadinessHealthCheck` with 5 sec cache, `CommandTimeout` 3 sec.
- `/health` — aggregated response.
- Graceful shutdown: readiness → 503 on SIGTERM (via `IHostApplicationLifetime`).

**Response:**

- Minimal JSON without details (`{"status": "Healthy"}`).
- No stack traces, IP addresses, service names.
- HTTP code: 200 (healthy) or 503 (unhealthy).

**Rate limiting on health branch:**

- 30 requests per 10 seconds.

**Exceptions from `/health/*`:**
Implemented via architecture (different ports 8080/8081):

- Request/Response logging — does NOT see `/health/*` requests.
- Correlation ID — does NOT see `/health/*` requests.
- Scalar UI (OpenAPI) — does NOT document `/health/*`.
- CORS — does NOT apply to `/health/*`.
- Exception handler — does NOT handle `/health/*` errors.

---

## 🔄 Graceful Shutdown

### What It Is

A mechanism for properly terminating the service when receiving a `SIGTERM` signal (from the orchestrator during deployment, scaling, or shutdown).

**Sequence:**

1. The service immediately stops accepting **new** requests (readiness → 503).
2. Waits for **current** requests to complete (within `terminationGracePeriodSeconds`).
3. Closes connections to DB, queues, cache.
4. Terminates the process.

**Why it's needed:** without graceful shutdown during deployment, users receive `502 Bad Gateway` — the load balancer is still sending traffic to a dead pod.

### How It's Implemented Here

Via `IHostApplicationLifetime.ApplicationStopping`. The readiness check examines the `IsCancellationRequested` flag **before** the cache — if `true`, it immediately returns `Unhealthy`. This gives the load balancer time to remove the pod from the pool before current request processing completes.

### Current State

- Uses `IHostApplicationLifetime.ApplicationStopping`.
- Readiness checks `IsCancellationRequested` **before** the cache — returns `Unhealthy` immediately when `true`.
- This gives the load balancer time to remove the pod before current request processing completes.

---

## 🚦 Rate Limiting

### What It Is

A mechanism for limiting the request frequency from a single client/IP per unit of time. Example: `30 requests per 10 seconds`.

**Why it's needed:**

- Protection against DDoS and brute force attacks.
- Protection of internal dependencies (DB, Redis) from exhaustion.
- Fair resource distribution among clients.

**Response when exceeded:** `429 Too Many Requests` with `Retry-After` header.

**Strategies:**

- **Fixed window** — counter resets every N seconds.
- **Sliding window** — sliding window, more accurate.
- **Token bucket** — tokens are replenished at a constant rate, allows bursts.

### How It's Implemented Here

Currently only on the health branch. API endpoints will be covered later.

### Current State

- On health branch: 30 requests per 10 seconds.

---

## 🗄️ Response Caching

### What It Is

Middleware for caching HTTP responses at the server level. Reduces API load and speeds up responses for clients. Works via standard HTTP headers:

- `Cache-Control: public, max-age=3600` — cache for 1 hour.
- `Age: 123` — how many seconds the response has been in the cache.
- `Vary` — which headers to distinguish cache by (e.g., `Authorization`).

**Why it's needed:**

- Reduces server load (no need to re-execute heavy logic).
- Speeds up client responses (cache is served instantly).
- Supports HTTP caching at the CDN, proxy, and browser level.

### How It's Implemented Here

- `builder.Services.AddResponseCaching()` — registration in DI.
- `app.UseResponseCaching()` — middleware in the pipeline (after Auth, before endpoints).
- `/api/metadata` — cached for 1 hour via `[ResponseCache(Duration = 3600)]`.

**Important:** middleware is placed after `UseAuthentication()` and `UseAuthorization()` to avoid caching 401/403 responses.

### Current State

- Middleware added to the pipeline.
- `/api/metadata` cached for 1 hour (public endpoint, `AllowAnonymous`).
- For protected endpoints caching is not configured — would require `VaryByHeader = "Authorization"`.

### Risks

| Risk                                       | Mitigation                                 |
| ------------------------------------------ | ------------------------------------------ |
| Cache serves 401/403 responses             | Middleware is placed after Auth            |
| Cache mixes responses from different users | Use `VaryByHeader = "Authorization"`       |
| Cache serves stale data                    | Configure `Duration` depending on endpoint |

---

## 🌐 CORS (Cross-Origin Resource Sharing)

### What It Is

A browser mechanism that allows or denies a web page from one domain to make requests to another domain's API. Controlled via HTTP headers:

- `Access-Control-Allow-Origin` — which domains are allowed.
- `Access-Control-Allow-Methods` — which HTTP methods.
- `Access-Control-Allow-Headers` — which headers.
- `Access-Control-Allow-Credentials` — whether cookies/authorization are allowed.

**Important:** CORS works **only in browsers**. Postman, curl, and server-side clients ignore it. This is not a security mechanism but a protection for browser users against unauthorized access from foreign sites.

### How It's Implemented Here

Currently an open policy for development. Before production, it needs to be locked down to specific frontend domains.

CORS does NOT apply to `/health/*` — health checks are on a separate port 8081.

### Current State

- Origins: `*` (all sources) — for development.
- Methods: GET, POST, PUT, DELETE.
- Headers: `*`.
- Credentials: not configured.

---

## 🛡️ Global Exception Handler

### What It Is

Middleware that intercepts **all** unhandled exceptions in the pipeline and returns a uniform safe JSON response to the client.

**Why it's needed:**

- Prevents leaking stack traces, IP addresses, DB/service names.
- Provides the client with a predictable error format.
- Logs full information on the server (for developers).

### How It's Implemented Here

Intercepts exceptions, maps them to HTTP codes and safe messages. Internal details (stack traces, IP, paths) are never exposed externally — only logged via `LogError`.

Exception handler does NOT handle `/health/*` errors — health checks are on a separate port 8081.

### Exception Mapping

| Exception                   | HTTP Code | Safe Message                    |
| --------------------------- | --------- | ------------------------------- |
| ArgumentException           | 400       | "Invalid request."              |
| KeyNotFoundException        | 404       | "Resource not found."           |
| UnauthorizedAccessException | 403       | "Access denied."                |
| TimeoutException            | 504       | "Request timed out."            |
| Any other                   | 500       | "An unexpected error occurred." |

### Response Format

```json
{
  "error": {
    "code": 400,
    "message": "Invalid request."
  }
}
```

### What We Don't Expose

- Stack traces.
- IP addresses.
- Service/DB names.
- Internal file paths.
- Full exception reason remains in logs (`LogError`).

### Files

- `Extensions/ExceptionHandler/ExceptionHandlerMiddleware.cs`
- `Extensions/ExceptionHandler/ExceptionHandlerExtensions.cs`

### Tests (7)

- `InvokeAsync_MapsExceptionToCorrectStatusCode` (5 cases).
- `InvokeAsync_ReturnsCorrectJsonFormat`.
- `InvokeAsync_DoesNotLeakInternalDetails`.

---

## ⚙️ Configuration

### What It Is

A centralized configuration system using Options classes with DataAnnotations validation. All settings are loaded from `appsettings.json`, validated at startup (fail-fast), and available via `IOptions<T>` in DI.

**Why it's needed:**

- Single source of truth for API metadata (used in OpenAPI, Scalar, `/api/metadata`).
- Fail-fast: if configuration is invalid — the application won't start.
- Type safety: `IOptions<AppSettings>` instead of `IConfiguration["AppSettings:Port"]`.
- Testability: `IOptions<T>` is easily mocked in unit tests.

### Options Classes

| Class                  | Section                 | Required Fields                        | Validation                                                     |
| ---------------------- | ----------------------- | -------------------------------------- | -------------------------------------------------------------- |
| `AppSettings`          | `AppSettings`           | ServiceName, Port                      | `[Required]`, `[Range(1, 65535)]`                              |
| `JwtOptions`           | `Jwt`                   | Key, Issuer, Audience                  | `[Required]`, `[MinLength(32)]` on Key                         |
| `OpenTelemetryOptions` | `OpenTelemetry`         | Endpoint                               | `[Required]` on Endpoint                                       |
| `ApiMetadataOptions`   | `ApiMetadata`           | Title, Version, Description, Developer | `[Required]`, `[StringLength]`, `[RegularExpression]` (semver) |
| `ContactInfo`          | `ApiMetadata:Developer` | Name, Url                              | `[Required]`, `[StringLength]`, `[Url]`, `[EmailAddress]`      |

### Validation

- **DataAnnotations** on properties: `[Required]`, `[Range]`, `[MinLength]`, `[StringLength]`, `[Url]`, `[EmailAddress]`, `[RegularExpression]`.
- **`ValidateOnStart()`** — fail-fast at startup if configuration is invalid.
- **`ValidateDataAnnotations()`** — automatic attribute checking.
- **`ConfigurationExtensions`** — registration via `AddOptions<T>().Bind(section).ValidateDataAnnotations().ValidateOnStart()`.

### Recursive Validation (Tests)

- Standard `Validator.TryValidateObject` does **NOT validate nested objects** (known .NET issue).
- `RecursiveValidator` (in tests) — custom helper for checking nested objects (e.g., `Developer` inside `ApiMetadataOptions`).
- In production, `ValidateOnStart()` is used — it recursively checks all nested objects automatically.

### Example: `appsettings.json`

```json
{
  "Kestrel": {
    "Endpoints": {
      "Api": {
        "Url": "http://0.0.0.0:8080"
      },
      "Health": {
        "Url": "http://0.0.0.0:8081"
      }
    }
  },
  "Cors": {
    "AllowedOrigins": ["*"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": false,
    "MaxAge": 3600
  },
  "AppSettings": {
    "ServiceName": "WebApiTemplate",
    "Port": 8080
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyAtLeast32CharactersLong!",
    "Issuer": "WebApiTemplate",
    "Audience": "WebApiTemplate"
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/WebApiTemplate.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14,
          "formatter": "Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"]
  },
  "OpenTelemetry": {
    "Endpoint": "http://otel-collector:4317",
    "Protocol": "Grpc",
    "Headers": {},
    "UseConsoleExporter": false,
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
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

### Fail-fast at Startup

In `Program.cs` the presence of required sections is checked:

```csharp
if (!builder.AddAppSettings())
{
    Console.WriteLine("[FATAL] AppSettings section is required");
    Log.Fatal("AppSettings section is required");
    Environment.Exit(1);
}
```

If the section is missing — the application exits with code 1 before the host starts.

### Tests

- `AppSettingsTests` (8 tests) — ServiceName and Port validation.
- `JwtOptionsTests` (9 tests) — Key, Issuer, Audience validation.
- `OpenTelemetryOptionsTests` (4 tests) — Endpoint validation, defaults.
- `ApiMetadataOptionsTests` (9 tests) — Title, Version, Description, Developer validation + nested.
- `ContactInfoTests` (12 tests) — Name, Email, Url validation.
- `ConfigurationExtensionsTests` (12 tests) — Options registration, fail-fast.
- `ObservabilityExtensionsTests` (5 tests) — Serilog and OpenTelemetry registration.

### Files

- `Configuration/Options/AppSettings.cs`
- `Configuration/Options/JwtOptions.cs`
- `Configuration/Options/OpenTelemetryOptions.cs`
- `Configuration/Options/ApiMetadataOptions.cs`
- `Configuration/Options/ContactInfo.cs`
- `Extensions/ConfigurationExtensions.cs`
- `Extensions/ObservabilityExtensions.cs`

---

## 📋 Metadata API

### What It Is

A public endpoint `GET /api/metadata` that returns API metadata: title, version, description, developer info. Used by the frontend to display the API version, footer, and about page.

### Why It's Needed

- **Single source of truth:** the same metadata is used in OpenAPI, Scalar UI, and `/api/metadata`.
- **Frontend gets data via API:** no need to hardcode the version on the client.
- **Changes without recompilation:** via `appsettings.json` or env vars.

### How It's Implemented

```csharp
app.MapGet("/api/metadata", (IOptions<ApiMetadataOptions> meta) =>
{
    var m = meta.Value;
    return Results.Ok(new
    {
        m.Title,
        m.Version,
        m.Description,
        Developer = new
        {
            m.Developer.Name,
            m.Developer.Email,
            m.Developer.Url
        }
    });
})
.WithName("GetApiMetadata")
.WithTags("Metadata")
.WithMetadata(new ResponseCacheAttribute { Duration = 3600 })
.AllowAnonymous();
```

### Current State

- Endpoint: `GET /api/metadata`
- Authentication: `AllowAnonymous` (public).
- Caching: 1 hour (`ResponseCacheAttribute`).
- Data source: `ApiMetadataOptions` (from `appsettings.json`).
- Response format: JSON with fields `title`, `version`, `description`, `developer`.

### Security

| Risk                | Level  | Mitigation                                      |
| ------------------- | ------ | ----------------------------------------------- |
| Secret disclosure   | Low    | Only public metadata is returned                |
| DDoS / overload     | Medium | Rate limiting + Response caching                |
| XSS via metadata    | Low    | Frontend escapes output (standard behavior)     |
| Unauthorized access | None   | Endpoint is public — API metadata is not secret |

### Response Example

```json
{
  "title": "SERVICE_TITLE_PLACEHOLDER",
  "version": "SERVICE_VERSION_PLACEHOLDER",
  "description": "SERVICE_DESCRIPTION_PLACEHOLDER",
  "developer": {
    "name": "AUTHOR_PLACEHOLDER",
    "email": "AUTHOR_EMAIL_PLACEHOLDER",
    "url": "AUTHOR_URL_PLACEHOLDER"
  }
}
```

---
