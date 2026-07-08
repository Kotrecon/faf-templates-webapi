# Конфигурация

## appsettings.json

### Основные настройки

```json
{
  "AppSettings": {
    "ServiceName": "WebApiTemplate",
    "Port": 8080
  }
}
```

### JWT

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyAtLeast32CharactersLong!",
    "Issuer": "WebApiTemplate",
    "Audience": "WebApiTemplate"
  }
}
```

### OpenTelemetry

```json
{
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
  }
}
```

### ApiMetadata

```json
{
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

## CORS

```json
{
  "Cors": {
    "AllowedOrigins": ["*"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": false,
    "MaxAge": 3600
  }
}
```

## Serilog

```json
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
          "path": "logs/webapi-template-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14,
          "formatter": "Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentName"]
  }
```

## Kestrel endpoints

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
  }
}
```

## Переменные окружения

Все настройки можно переопределить через env vars:

- `AppSettings__Port=8082`
- `Jwt__Key=NewSecretKey`
- `OpenTelemetry__Endpoint=http://new-otel:4317`
