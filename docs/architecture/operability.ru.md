# Operability — Эксплуатационная готовность сервиса WebApiTemplate

| Поле       | Значение                    |
| ---------- | --------------------------- |
| **Версия** | SERVICE_VERSION_PLACEHOLDER |
| **Статус** | Active                      |
| **Дата**   | 2026-07-08                  |

> Документ описывает, как сервис ведёт себя в продакшене: как его мониторят, как он перезапускается, как защищается от перегрузок и как обрабатывает ошибки. Предназначен для разработчиков, DevOps и SRE.

---

## 🩺 Health Checks

### Что это

HTTP-эндпоинты, которые оркестратор (Kubernetes, Docker) регулярно опрашивает, чтобы понять состояние сервиса. Бывают трёх типов:

- **Liveness** (`/health/live`) — «процесс жив?» Если нет → контейнер перезапускается.
- **Readiness** (`/health/ready`) — «готов принимать трафик?» Если нет → трафик уходит на другие поды, но под НЕ перезапускается.
- **Startup** (`/health`) — «завершил ли инициализацию?» Используется при медленном старте (прогрев кэша, миграции).

**Главное правило:** liveness должен быть максимально лёгким (без БД/Redis), иначе падение зависимости вызовет каскадный перезапуск всех подов.

### Сравнение health-эндпоинтов

| Характеристика                           | `/health/live` (Liveness)                                                                                                                                                              | `/health/ready` (Readiness)                                                                                                                                                                        | `/health` (Base)                                                                                                                                                                  |
| :--------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Основной вопрос**                      | Работает ли процесс приложения или он завис/умер?                                                                                                                                      | Готов ли сервис принимать и обрабатывать пользовательский трафик прямо сейчас?                                                                                                                     | Какое общее состояние сервиса и всех его зависимостей?                                                                                                                            |
| **Что проверяет**                        | Только то, что HTTP-сервер жив и может принять запрос. Никаких проверок БД, Redis, внешних API. Проверяет event-loop, отсутствие deadlock'ов, утечек памяти.                           | Наличие активных соединений с критичными зависимостями: БД (проверка connection pool), Redis/Kafka, завершение стартовых процедур (прогрев кэша, загрузка конфигов, инициализация service mesh).   | Агрегирует проверки Liveness и Readiness, возвращает детальный статус каждой зависимости (БД, кэш, очереди, внешние API), версию сервиса, uptime, метрики использования ресурсов. |
| **HTTP-коды ответа**                     | `200 OK` — процесс жив<br>`503 Service Unavailable` — процесс завис                                                                                                                    | `200 OK` — готов принимать трафик<br>`503 Service Unavailable` — зависимости недоступны, не готов                                                                                                  | `200 OK` — все системы работают<br>`503 Service Unavailable` — критичные зависимости упали                                                                                        |
| **Кто опрашивает**                       | Kubelet (Kubernetes), Docker healthcheck, systemd watchdog                                                                                                                             | Kube-proxy, Ingress Controller, Service Mesh (Istio, Linkerd), внутренние Load Balancer (AWS ALB, Yandex LB)                                                                                       | Prometheus (blackbox exporter), Zabbix, UptimeRobot, Grafana дашборды, админские панели мониторинга                                                                               |
| **Реакция на 503 / Timeout**             | **Restart:** Оркестратор убивает контейнер (kill -9) и перезапускает его. Счетчик рестартов увеличивается. При превышении лимита (CrashLoopBackOff) — под переходит в состояние Error. | **Remove:** Под удаляется из endpoints/balancer pool. Трафик перенаправляется на другие здоровые поды. Под НЕ перезапускается. Когда зависимость восстановится — под автоматически вернется в пул. | **Alert:** Мониторинг фиксирует инцидент, рисует красный статус на дашборде, отправляет уведомление в Slack/PagerDuty/Telegram дежурному инженеру. На маршрутизацию не влияет.    |
| **Требования к скорости ответа**         | Должен отвечать за **< 100мс**. Любая задержка = риск ложного restart'а.                                                                                                               | Может отвечать дольше — **до 1-3 секунд**. Допустимы задержки при проверке пула соединений.                                                                                                        | Не критично — **до 5-10 секунд**. Используется для мониторинга, не влияет на маршрутизацию.                                                                                       |
| **Частота опроса**                       | Каждые **5-10 секунд** (настраивается в `periodSeconds`)                                                                                                                               | Каждые **5-10 секунд** (настраивается в `periodSeconds`)                                                                                                                                           | Каждые **30-60 секунд** или по запросу (pull-модель Prometheus)                                                                                                                   |
| **Timeout**                              | Обычно **1-2 секунды**. Если не ответил за это время — считается failed.                                                                                                               | Обычно **3-5 секунд**. Дается время на проверку зависимостей.                                                                                                                                      | Обычно **10-30 секунд**. Допустимы долгие проверки.                                                                                                                               |
| **Опасности при неправильной настройке** | Если проверять БД здесь — при падении БД Kubernetes будет бесконечно перезапускать поды (Cascading Failure).                                                                           | Если сделать слишком тяжелым (JOIN'ы, сложные запросы) — сам healthcheck начнет нагружать БД и может ее положить.                                                                                  | Если отдавать детали (IP БД, стек-трейсы) в публичную сеть — утечка информации для атакующих.                                                                                     |
| **Best Practices**                       | - Никаких внешних зависимостей<br>- Минимальная логика<br>- Сетевая изоляция через отдельный порт (8081)                                                                               | - Проверять только критичные зависимости<br>- Использовать `SELECT 1` вместо тяжелых запросов<br>- Кэшировать результаты на 5 секунд<br>- Rate limiting                                            | - Санитизировать ответы (не отдавать internals)<br>- Порт закрыт от внешней сети на уровне инфраструктуры<br>- Возвращать только необходимую информацию                           |

### Как реализовано у нас

**Архитектурное решение:** health checks вынесены на отдельный порт `8081`, API работает на `8080`. Это даёт автоматическую изоляцию от middleware (logging, correlation ID, CORS, exception handler, Scalar) — они просто не видят health-запросы.

### Текущее состояние

**Порты:**

- Два Kestrel-эндпоинта: API (8080) и Health (8081) — `appsettings.json`.

**Эндпоинты:**

- `/health/live` — delegate, без отдельного класса, без зависимостей.
- `/health/ready` — `ReadinessHealthCheck` с кэшем 5 сек, `CommandTimeout` 3 сек.
- `/health` — агрегированный ответ.
- Graceful shutdown: readiness → 503 при SIGTERM (через `IHostApplicationLifetime`).

**Ответ:**

- Минимальный JSON без деталей (`{"status": "Healthy"}`).
- Без stack traces, IP-адресов, названий сервисов.
- HTTP-код: 200 (healthy) или 503 (unhealthy).

**Rate limiting на health-ветке:**

- 30 запросов за 10 секунд.

**Исключения из `/health/*`:**
Реализованы через архитектуру (разные порты 8080/8081):

- Request/Response logging — НЕ видит запросов `/health/*`.
- Correlation ID — НЕ видит запросов `/health/*`.
- Scalar UI (OpenAPI) — НЕ документирует `/health/*`.
- CORS — НЕ применяется к `/health/*`.
- Exception handler — НЕ обрабатывает ошибки `/health/*`.

---

## 🔄 Graceful Shutdown

### Что это

Механизм корректного завершения работы сервиса при получении сигнала `SIGTERM` (от оркестратора при деплое, масштабировании или остановке).

**Последовательность:**

1. Сервис сразу перестаёт принимать **новые** запросы (readiness → 503).
2. Дожидается завершения **текущих** запросов (в пределах `terminationGracePeriodSeconds`).
3. Закрывает соединения с БД, очередями, кэшем.
4. Завершает процесс.

**Зачем нужно:** без graceful shutdown при деплое пользователи получают `502 Bad Gateway` — балансировщик ещё шлёт трафик в уже мёртвый под.

### Как реализовано у нас

Через `IHostApplicationLifetime.ApplicationStopping`. Readiness-чек проверяет флаг `IsCancellationRequested` **до** кэша — если `true`, сразу возвращает `Unhealthy`. Это даёт балансировщику время убрать под из пула до завершения обработки текущих запросов.

### Текущее состояние

- Используется `IHostApplicationLifetime.ApplicationStopping`.
- Readiness проверяет `IsCancellationRequested` **до** кэша — при `true` сразу возвращает `Unhealthy`.
- Это даёт балансировщику время убрать под до завершения обработки текущих запросов.

---

## 🚦 Rate Limiting

### Что это

Механизм ограничения частоты запросов от одного клиента/IP за единицу времени. Пример: `30 запросов за 10 секунд`.

**Зачем нужен:**

- Защита от DDoS и брутфорса.
- Защита внутренних зависимостей (БД, Redis) от истощения.
- Справедливое распределение ресурсов между клиентами.

**Ответ при превышении:** `429 Too Many Requests` с заголовком `Retry-After`.

**Стратегии:**

- **Fixed window** — счётчик сбрасывается каждые N секунд.
- **Sliding window** — скользящее окно, более точный.
- **Token bucket** — токены пополняются с постоянной скоростью, допускает bursts.

### Как реализовано у нас

Пока только на health-ветке. API-эндпоинты будут покрыты позже.

### Текущее состояние

- На health-ветку: 30 запросов за 10 секунд.

---

## 🗄️ Response Caching

### Что это

Middleware для кэширования HTTP-ответов на уровне сервера. Позволяет уменьшить нагрузку на API и ускорить ответ для клиентов. Работает через стандартные HTTP-заголовки:

- `Cache-Control: public, max-age=3600` — кэшировать на 1 час.
- `Age: 123` — сколько секунд ответ находится в кэше.
- `Vary` — по каким заголовкам различать кэш (например, `Authorization`).

**Зачем нужен:**

- Снижение нагрузки на сервер (не выполнять тяжёлую логику повторно).
- Ускорение ответа для клиентов (кэш отдаётся мгновенно).
- Поддержка HTTP-кэширования на уровне CDN, прокси, браузера.

### Как реализовано у нас

- `builder.Services.AddResponseCaching()` — регистрация в DI.
- `app.UseResponseCaching()` — middleware в pipeline (после Auth, до endpoints).
- `/api/metadata` — кэшируется на 1 час через `[ResponseCache(Duration = 3600)]`.

**Важно:** middleware стоит после `UseAuthentication()` и `UseAuthorization()`, чтобы не кэшировать ответы 401/403.

### Текущее состояние

- Middleware добавлен в pipeline.
- `/api/metadata` кэшируется на 1 час (публичный endpoint, `AllowAnonymous`).
- Для защищённых endpoints кэш не настроен — потребует `VaryByHeader = "Authorization"`.

### Риски

| Риск                                      | Митигация                                        |
| ----------------------------------------- | ------------------------------------------------ |
| Кэш отдаёт ответы 401/403                 | Middleware стоит после Auth                      |
| Кэш смешивает ответы разных пользователей | Использовать `VaryByHeader = "Authorization"`    |
| Кэш отдаёт устаревшие данные              | Настраивать `Duration` в зависимости от endpoint |

---

## 🌐 CORS (Cross-Origin Resource Sharing)

### Что это

Механизм браузера, который разрешает или запрещает веб-странице с одного домена делать запросы на API другого домена. Контролируется HTTP-заголовками:

- `Access-Control-Allow-Origin` — какие домены разрешены.
- `Access-Control-Allow-Methods` — какие HTTP-методы.
- `Access-Control-Allow-Headers` — какие заголовки.
- `Access-Control-Allow-Credentials` — разрешены ли куки/авторизация.

**Важно:** CORS работает **только в браузере**. Postman, curl, серверные клиенты его игнорируют. Это не механизм безопасности, а защита пользователей браузера от несанкционированного доступа с чужих сайтов.

### Как реализовано у нас

Сейчас открытая политика для разработки. Перед продакшеном нужно закрыть на конкретные домены фронта.

CORS НЕ применяется к `/health/*` — health checks на отдельном порту 8081.

### Текущее состояние

- Origins: `*` (все источники) — для разработки.
- Methods: GET, POST, PUT, DELETE.
- Headers: `*`.
- Credentials: не настроены.

---

## 🛡️ Global Exception Handler

### Что это

Middleware, который перехватывает **все** необработанные исключения в pipeline и возвращает клиенту единообразный безопасный JSON.

**Зачем нужен:**

- Не даёт утечь stack trace, IP-адресам, названиям БД/сервисов.
- Даёт клиенту предсказуемый формат ошибки.
- Логирует полную информацию на сервере (для разработчиков).

### Как реализовано у нас

Перехватывает исключения, маппит их на HTTP-коды и безопасные сообщения. Внутренние детали (stack trace, IP, пути) никогда не отдаются наружу — только в логи через `LogError`.

Exception handler НЕ обрабатывает ошибки `/health/*` — health checks на отдельном порту 8081.

### Маппинг исключений

| Исключение                  | HTTP-код | Безопасное сообщение            |
| --------------------------- | -------- | ------------------------------- |
| ArgumentException           | 400      | "Invalid request."              |
| KeyNotFoundException        | 404      | "Resource not found."           |
| UnauthorizedAccessException | 403      | "Access denied."                |
| TimeoutException            | 504      | "Request timed out."            |
| Любое другое                | 500      | "An unexpected error occurred." |

### Формат ответа

```json
{
  "error": {
    "code": 400,
    "message": "Invalid request."
  }
}
```

### Что НЕ отдаём

- Stack traces.
- IP-адреса.
- Названия сервисов/БД.
- Внутренние пути файлов.
- Полная причина исключения остаётся в логах (`LogError`).

### Файлы

- `Extensions/ExceptionHandler/ExceptionHandlerMiddleware.cs`
- `Extensions/ExceptionHandler/ExceptionHandlerExtensions.cs`

### Тесты (7)

- `InvokeAsync_MapsExceptionToCorrectStatusCode` (5 кейсов).
- `InvokeAsync_ReturnsCorrectJsonFormat`.
- `InvokeAsync_DoesNotLeakInternalDetails`.

---

## ⚙️ Configuration

### Что это

Централизованная система конфигурации через Options-классы с DataAnnotations-валидацией. Все настройки загружаются из `appsettings.json`, валидируются при старте (fail-fast), и доступны через `IOptions<T>` в DI.

**Зачем нужно:**

- Единый источник истины для метаданных API (используется в OpenAPI, Scalar, `/api/metadata`).
- Fail-fast: если конфигурация невалидна — приложение не стартует.
- Типобезопасность: `IOptions<AppSettings>` вместо `IConfiguration["AppSettings:Port"]`.
- Тестируемость: `IOptions<T>` легко мокается в unit-тестах.

### Options-классы

| Класс                  | Секция                  | Обязательные поля                      | Валидация                                                      |
| ---------------------- | ----------------------- | -------------------------------------- | -------------------------------------------------------------- |
| `AppSettings`          | `AppSettings`           | ServiceName, Port                      | `[Required]`, `[Range(1, 65535)]`                              |
| `JwtOptions`           | `Jwt`                   | Key, Issuer, Audience                  | `[Required]`, `[MinLength(32)]` на Key                         |
| `OpenTelemetryOptions` | `OpenTelemetry`         | Endpoint                               | `[Required]` на Endpoint                                       |
| `ApiMetadataOptions`   | `ApiMetadata`           | Title, Version, Description, Developer | `[Required]`, `[StringLength]`, `[RegularExpression]` (semver) |
| `ContactInfo`          | `ApiMetadata:Developer` | Name, Url                              | `[Required]`, `[StringLength]`, `[Url]`, `[EmailAddress]`      |

### Валидация

- **DataAnnotations** на properties: `[Required]`, `[Range]`, `[MinLength]`, `[StringLength]`, `[Url]`, `[EmailAddress]`, `[RegularExpression]`.
- **`ValidateOnStart()`** — fail-fast при старте, если конфигурация невалидна.
- **`ValidateDataAnnotations()`** — автоматическая проверка атрибутов.
- **`ConfigurationExtensions`** — регистрация через `AddOptions<T>().Bind(section).ValidateDataAnnotations().ValidateOnStart()`.

### Рекурсивная валидация (тесты)

- Стандартный `Validator.TryValidateObject` **НЕ валидирует вложенные объекты** (известная проблема .NET).
- `RecursiveValidator` (в тестах) — кастомный helper для проверки nested объектов (например, `Developer` внутри `ApiMetadataOptions`).
- В production используется `ValidateOnStart()` — он рекурсивно проверяет все вложенные объекты автоматически.

### Пример: `appsettings.json`

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

### Fail-fast при старте

В `Program.cs` проверяется наличие обязательных секций:

```csharp
if (!builder.AddAppSettings())
{
    Console.WriteLine("[FATAL] AppSettings section is required");
    Log.Fatal("AppSettings section is required");
    Environment.Exit(1);
}
```

Если секция отсутствует — приложение завершается с кодом 1 до запуска хоста.

### Тесты

- `AppSettingsTests` (8 тестов) — валидация ServiceName и Port.
- `JwtOptionsTests` (9 тестов) — валидация Key, Issuer, Audience.
- `OpenTelemetryOptionsTests` (4 теста) — валидация Endpoint, defaults.
- `ApiMetadataOptionsTests` (9 тестов) — валидация Title, Version, Description, Developer + nested.
- `ContactInfoTests` (12 тестов) — валидация Name, Email, Url.
- `ConfigurationExtensionsTests` (12 тестов) — регистрация Options, fail-fast.
- `ObservabilityExtensionsTests` (5 тестов) — регистрация Serilog и OpenTelemetry.

### Файлы

- `Configuration/Options/AppSettings.cs`
- `Configuration/Options/JwtOptions.cs`
- `Configuration/Options/OpenTelemetryOptions.cs`
- `Configuration/Options/ApiMetadataOptions.cs`
- `Configuration/Options/ContactInfo.cs`
- `Extensions/ConfigurationExtensions.cs`
- `Extensions/ObservabilityExtensions.cs`

---

## 📋 Metadata API

### Что это

Публичный endpoint `GET /api/metadata`, который возвращает метаданные API: title, version, description, developer info. Используется фронтендом для отображения версии API, footer, about-страницы.

### Зачем нужен

- **Единый источник истины:** одни и те же метаданные используются в OpenAPI, Scalar UI, `/api/metadata`.
- **Фронтенд получает данные через API:** не нужно хардкодить версию на клиенте.
- **Меняется без перекомпиляции:** через `appsettings.json` или env vars.

### Как реализовано

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

### Текущее состояние

- Endpoint: `GET /api/metadata`
- Аутентификация: `AllowAnonymous` (публичный).
- Кэширование: 1 час (`ResponseCacheAttribute`).
- Источник данных: `ApiMetadataOptions` (из `appsettings.json`).
- Формат ответа: JSON с полями `title`, `version`, `description`, `developer`.

### Безопасность

| Риск                       | Уровень | Митигация                                     |
| -------------------------- | ------- | --------------------------------------------- |
| Раскрытие секретов         | Низкий  | Возвращаем только публичные метаданные        |
| DDoS / перегрузка          | Средний | Rate limiting + Response caching              |
| XSS через метаданные       | Низкий  | Фронтенд экранирует вывод (стандартно)        |
| Несанкционированный доступ | Нет     | Эндпоинт публичный — метаданные API не секрет |

### Пример ответа

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
