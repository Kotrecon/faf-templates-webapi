# Deployment — Развёртывание сервиса WebApiTemplate

| Поле       | Значение                    |
| ---------- | --------------------------- |
| **Сервис** | WebApiTemplate              |
| **Версия** | SERVICE_VERSION_PLACEHOLDER |
| **Статус** | Active                      |
| **Дата**   | 2026-07-08                  |

---

## Порты

| Порт | Назначение                          | Доступ                          |
| ---- | ----------------------------------- | ------------------------------- |
| 8080 | API (REST, OpenAPI, Scalar)         | Публичный (через reverse proxy) |
| 8081 | Health checks (liveness, readiness) | Только внутренняя сеть          |

> 💡 Порт API (8080) заменяется параметром `--Port` при создании проекта из шаблона.

---

## Environment Variables

### Обязательные

| Переменная               | Описание                             | Пример                                       |
| ------------------------ | ------------------------------------ | -------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT` | Окружение                            | `Production`                                 |
| `Jwt__Key`               | JWT signing key (минимум 32 символа) | Генерировать через `openssl rand -base64 32` |

### Опциональные

| Переменная                | По умолчанию               | Описание                     |
| ------------------------- | -------------------------- | ---------------------------- |
| `Cors__AllowedOrigins__0` | `*` (только в Development) | Разрешённый origin для CORS  |
| `OpenTelemetry__Endpoint` | `http://localhost:4317`    | OTLP endpoint для телеметрии |

---

## Генерация JWT-ключа

```bash
# Linux/macOS
openssl rand -base64 32

# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }) -as [byte[]])
```

---

## Reverse Proxy (пример Nginx)

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

    # Health-порт (8081) НЕ проксируется — только изнутри кластера
}
```

---

## Graceful Shutdown

Сервис обрабатывает `SIGTERM` корректно через `IHostApplicationLifetime`:

- Health check `/health/ready` возвращает `Unhealthy` при shutdown
- Kestrel ждёт завершения текущих запросов
- `terminationGracePeriodSeconds: 30` в Kubernetes

---

## Использование

```powershell
# Дефолт (api.example.com)
dotnet new faf-webapi -n OrderService

# Свой хост
dotnet new faf-webapi -n OrderService --ServiceHost "orders.mycompany.com"
```

---
