# API — Текущее состояние

| Поле       | Значение                    |
| ---------- | --------------------------- |
| **Версия** | SERVICE_VERSION_PLACEHOLDER |
| **Статус** | Active                      |
| **Дата**   | 2026-07-08                  |
| **API**    | v1 (URL-based versioning)   |

> Документ описывает публичные и внутренние API-эндпоинты сервиса, их аутентификацию, форматы запросов/ответов и коды ошибок.
>
> Детали реализации health checks — в [`operability.md`](operability.md).

---

## Общие принципы

### Версионирование API

**Стратегия:** URL-based versioning через `Asp.Versioning.Mvc` 10.0.0.

**Формат:** `/api/v{version}/[controller]`

**Текущая версия:** 1.0

**Политика совместимости:**

| Изменение                             | Тип   | Версия |
| ------------------------------------- | ----- | ------ |
| Добавление нового эндпоинта           | Minor | v1.x   |
| Добавление опционального поля в ответ | Minor | v1.x   |
| Изменение обязательного поля          | Major | v2.0   |
| Удаление эндпоинта                    | Major | v2.0   |
| Изменение типа поля                   | Major | v2.0   |

**Поддерживаемые версии:**

| Версия | Статус | Дата релиза | EOL |
| ------ | ------ | ----------- | --- |
| v1.0   | Active | 2026-07-08  | —   |

**Заголовки ответа:**

```http
api-supported-versions: 1.0
api-deprecated-versions:
```

### Аутентификация

**API (порт 8080):**
Все эндпоинты (кроме dev, metadata и OpenAPI) требуют JWT Bearer токен в заголовке:

```bash
Authorization: Bearer <token>
```

**Health Checks (порт 8081):**
Аутентификация не требуется. Защита — сетевая изоляция (порт не маршрутизируется наружу).

**Dev API (порт 8080, только Development):**
Аутентификация не требуется. Используется для генерации тестовых JWT-токенов.

**Metadata API (порт 8080):**
Аутентификация не требуется. Публичный endpoint с метаданными API.

### Корреляция

Заголовок `X-Correlation-Id` автоматически генерируется (`Guid.CreateVersion7`) или прокидывается из входящего запроса. Доступен в логах и OTel-трейсах.

### OpenAPI UI

В окружении Development доступен интерактивный UI для тестирования API:

| URL                                     | Описание                               |
| --------------------------------------- | -------------------------------------- |
| `http://localhost:8080/scalar/v1`       | Scalar UI — интерактивная документация |
| `http://localhost:8080/openapi/v1.json` | OpenAPI 3.1 документ                   |

**Стек:** `Microsoft.AspNetCore.OpenApi` 10.0.9 + `Scalar.AspNetCore` 2.13.19

JWT-авторизация встроена в OpenAPI-документ через `DocumentTransformer` — кнопка **Authorize** в UI.

**Конфигурация:**

- Title, Version, Description берутся из `ApiMetadataOptions` (appsettings.json)
- Contact информация — developer contact из конфигурации
- Security scheme: Bearer JWT, автоматически применяется ко всем эндпоинтам

---

## Эндпоинты

### API (порт 8080)

| Метод | Роут                         | Описание                     | Доступ      |
| ----- | ---------------------------- | ---------------------------- | ----------- |
| GET   | `/api/v1/logging/level`      | Текущие уровни логирования   | AuditViewer |
| PUT   | `/api/v1/logging/level`      | Изменить уровень логирования | AdminOnly   |
| GET   | `/api/v1/logging/categories` | Список категорий с overrides | AuditViewer |
| GET   | `/api/metadata`              | Метаданные API               | Анонимный   |

### Dev API (порт 8080, только Development)

| Метод | Роут         | Описание                       | Доступ    |
| ----- | ------------ | ------------------------------ | --------- |
| POST  | `/dev/token` | Генерация тестового JWT-токена | Анонимный |

### Health Checks (порт 8081 — внутренний)

| Метод | Роут            | Описание               | Доступ     |
| ----- | --------------- | ---------------------- | ---------- |
| GET   | `/health/live`  | Процесс жив            | Внутренний |
| GET   | `/health/ready` | Готов принимать трафик | Внутренний |
| GET   | `/health`       | Агрегированный ответ   | Внутренний |

---

## Формат ошибок (RFC 7807 ProblemDetails)

Все бизнес-ошибки возвращаются в формате **RFC 7807 ProblemDetails** через Result Pattern.

### Структура ответа

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation failed",
  "status": 422,
  "detail": "One or more validation errors occurred",
  "instance": "/api/v1/scenarios",
  "errors": [
    {
      "code": "ValidationFailed",
      "message": "Name is required"
    }
  ],
  "validationErrors": {
    "name": ["Name is required", "Name must be at least 3 characters"]
  }
}
```

### Типы ошибок

| Тип ошибки        | HTTP-код | Когда используется                          |
| ----------------- | -------- | ------------------------------------------- |
| ValidationError   | 422      | Ошибки валидации входных данных             |
| NotFoundError     | 404      | Ресурс не найден                            |
| ConflictError     | 409      | Конфликт (дубликат, нарушение уникальности) |
| ForbiddenError    | 403      | Доступ запрещён                             |
| BusinessRuleError | 400      | Нарушение бизнес-правила                    |

### Технические ошибки

Необработанные исключения перехватываются `ExceptionHandlerMiddleware` и возвращаются в упрощённом формате:

```json
{
  "error": {
    "code": 500,
    "message": "An unexpected error occurred"
  }
}
```

Внутренние детали (stack trace, IP, пути) **никогда** не отдаются клиенту — только в логи.

---

## Dev API

### POST /dev/token

Генерирует тестовый JWT-токен. **Только для окружения Development.**

**Запрос:**

```json
{
  "username": "admin",
  "roles": ["Admin"]
}
```

| Поле     | Тип      | Обязательно | Описание                        |
| -------- | -------- | ----------- | ------------------------------- |
| username | string   | Да          | Имя пользователя (claim `name`) |
| roles    | string[] | Да          | Массив ролей (claim `role`)     |

**Допустимые роли:** `Admin`, `Operator`, `Auditor`

**Ответ (200 OK):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": 8
}
```

| Поле    | Тип    | Описание                       |
| ------- | ------ | ------------------------------ |
| token   | string | JWT-токен (HS256, 8 часов TTL) |
| expires | int    | Срок действия в часах          |

**Пример получения токена (PowerShell):**

```powershell
$token = (Invoke-RestMethod -Method POST `
  -Uri http://localhost:8080/dev/token `
  -ContentType "application/json" `
  -Body '{"username":"admin","roles":["Admin"]}').token
```

---

## Metadata API

### GET /api/metadata

Публичный endpoint — метаданные API не являются секретом. Не требует аутентификации.

**Ответ (200 OK):**

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

| Поле        | Тип    | Описание                         |
| ----------- | ------ | -------------------------------- |
| title       | string | Название API                     |
| version     | string | Версия API (semver)              |
| description | string | Описание API                     |
| developer   | object | Контактные данные разработчика   |
| name        | string | Имя разработчика                 |
| email       | string | Email разработчика (опционально) |
| url         | string | URL разработчика (опционально)   |

**Конфигурация:**

Значения берутся из `ApiMetadataOptions` в `appsettings.json`:

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

**Кэширование:** 1 час (`ResponseCache(Duration = 3600)`)

**Коды ответа:**

| Код | Описание |
| --- | -------- |
| 200 | Успешно  |

---

## Logging API

### GET /api/v1/logging/level

Возвращает текущие уровни логирования.

**Ответ (200 OK):**

```json
{
  "default": "Information",
  "overrides": {
    "Microsoft": "Warning",
    "System": "Warning"
  }
}
```

| Код | Описание                        |
| --- | ------------------------------- |
| 200 | Успешно                         |
| 401 | Не аутентифицирован             |
| 403 | Нет роли Admin/Operator/Auditor |

### PUT /api/v1/logging/level

Изменяет уровень логирования для категории или root level.

**Запрос:**

```json
{
  "category": "Microsoft.AspNetCore",
  "level": "Warning"
}
```

| Поле     | Тип    | Обязательно | Описание                      |
| -------- | ------ | ----------- | ----------------------------- |
| category | string | Нет         | Категория (null = root level) |
| level    | string | Да          | Уровень логирования           |

**Допустимые уровни:** Debug, Information, Warning, Error  
**Запрещённые уровни:** Fatal, Verbose (безопасность)

**Ответы:**

- `200 OK` — уровень изменён (пустое тело)

- `400 Bad Request` — невалидный уровень (ProblemDetails):

  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "Invalid request",
    "status": 400,
    "errors": [
      {
        "code": "BusinessRuleViolation",
        "message": "Invalid level: Critical"
      }
    ]
  }
  ```

- `404 Not Found` — категория не найдена (ProblemDetails):

  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.4.5",
    "title": "Resource not found",
    "status": 404,
    "errors": [
      {
        "code": "NotFound",
        "message": "Category NonExistent not found"
      }
    ]
  }
  ```

- `422 Unprocessable Entity` — ошибка валидации (ProblemDetails):

  ```json
  {
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.22",
    "title": "Validation failed",
    "status": 422,
    "errors": [
      {
        "code": "ValidationFailed",
        "message": "Level is required"
      }
    ],
    "validationErrors": {
      "level": ["Level is required"]
    }
  }
  ```

| Код | Описание             |
| --- | -------------------- |
| 200 | Уровень изменён      |
| 400 | Невалидный запрос    |
| 401 | Не аутентифицирован  |
| 403 | Нет роли Admin       |
| 404 | Категория не найдена |
| 422 | Ошибка валидации     |

### GET /api/v1/logging/categories

Возвращает список категорий с overrides.

**Ответ (200 OK):**

```json
["Microsoft", "System"]
```

| Код | Описание                        |
| --- | ------------------------------- |
| 200 | Успешно                         |
| 401 | Не аутентифицирован             |
| 403 | Нет роли Admin/Operator/Auditor |

---

## Health Checks API

> Детали реализации (что проверяет каждый эндпоинт, graceful shutdown, rate limiting) — в [`operability.md`](operability.md).

### Запрос

Все эндпоинты принимают GET-запросы без тела:

```bash
GET /health/live
GET /health/ready
GET /health
```

### Ответы

**200 OK (Healthy):**

```json
{
  "status": "Healthy"
}
```

**503 Service Unavailable (Unhealthy):**

```json
{
  "status": "Unhealthy"
}
```

**429 Too Many Requests (Rate Limit):**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.16",
  "title": "Too Many Requests",
  "status": 429
}
```

### Коды ответа

| Код | Описание                               |
| --- | -------------------------------------- |
| 200 | Процесс жив / зависимости готовы       |
| 429 | Rate limit превышен (30 req/10s)       |
| 503 | Процесс завис / зависимости недоступны |

---

## Коды ошибок (общие)

| Код | Описание                   |
| --- | -------------------------- |
| 400 | Невалидный запрос          |
| 401 | Не аутентифицирован        |
| 403 | Нет прав (policy-based)    |
| 404 | Ресурс не найден           |
| 422 | Ошибка валидации           |
| 429 | Rate limit превышен        |
| 503 | Сервис недоступен (health) |

---

## Политика доступа (ролевая матрица)

| Роль     | AuditViewer (чтение) | AdminOnly (изменение) |
| -------- | :------------------: | :-------------------: |
| Admin    |          ✅          |          ✅           |
| Operator |          ✅          |          ❌           |
| Auditor  |          ✅          |          ❌           |

---
