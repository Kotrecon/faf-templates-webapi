# Быстрый старт

## Установка шаблона

Из NuGet.org:

```bash
dotnet new install Faf.Templates.WebApi
```

Из локального фида:

```bash
dotnet new install Faf.Templates.WebApi --source E:\projects\shared\.nuget-feed\templates
```

## Создание проекта

```bash
# Базовый
dotnet new faf-webapi -n OrderService

# С параметрами
dotnet new faf-webapi -n PaymentService \
    --port 8082 \
    --author "Иван Петров" \
    --description "Сервис обработки платежей"

# Без тестов
dotnet new faf-webapi -n SimpleService --no-tests
```

## Запуск

```bash
cd OrderService
dotnet run
```

API доступен на `http://localhost:8080`, health checks на `http://localhost:8081`.

## Проверка

```bash
# Scalar UI (OpenAPI документация)
http://localhost:8080/scalar

# Health check
curl http://localhost:8081/health

# Metadata
curl http://localhost:8080/api/metadata
```

---
