# Deployment — WebApiTemplate Service Deployment

| Field       | Value                       |
| ----------- | --------------------------- |
| **Service** | WebApiTemplate              |
| **Version** | SERVICE_VERSION_PLACEHOLDER |
| **Status**  | Active                      |
| **Date**    | 2026-07-08                  |

---

## Ports

| Port | Purpose                             | Access                     |
| ---- | ----------------------------------- | -------------------------- |
| 8080 | API (REST, OpenAPI, Scalar)         | Public (via reverse proxy) |
| 8081 | Health checks (liveness, readiness) | Internal network only      |

> 💡 The API port (8080) can be overridden with the `--Port` parameter when creating a project from the template.

---

## Environment Variables

### Required

| Variable                 | Description                             | Example                                |
| ------------------------ | --------------------------------------- | -------------------------------------- |
| `ASPNETCORE_ENVIRONMENT` | Environment                             | `Production`                           |
| `Jwt__Key`               | JWT signing key (minimum 32 characters) | Generate via `openssl rand -base64 32` |

### Optional

| Variable                  | Default                 | Description                 |
| ------------------------- | ----------------------- | --------------------------- |
| `Cors__AllowedOrigins__0` | `*` (Development only)  | Allowed origin for CORS     |
| `OpenTelemetry__Endpoint` | `http://localhost:4317` | OTLP endpoint for telemetry |

---

## JWT Key Generation

```bash
# Linux/macOS
openssl rand -base64 32

# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }) -as [byte[]])
```

---

## Reverse Proxy (Nginx Example)

```nginx
upstream web_api_template {
    server 127.0.0.1:8080;
}

server {
    listen 443 ssl;
    server_name SERVICE_HOST_PLACEHOLDER;

    location / {
        proxy_pass http://web_api_template;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Health port (8081) is NOT proxied — internal cluster access only
}
```

---

## Graceful Shutdown

The service handles `SIGTERM` correctly via `IHostApplicationLifetime`:

- The `/health/ready` health check returns `Unhealthy` during shutdown
- Kestrel waits for in-flight requests to complete
- `terminationGracePeriodSeconds: 30` in Kubernetes

---

## Usage

```powershell
# Default (api.example.com)
dotnet new faf-webapi -n OrderService

# Custom host
dotnet new faf-webapi -n OrderService --ServiceHost "orders.mycompany.com"
```

---
