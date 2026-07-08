# Auth Flow — Аутентификация и Авторизация

| Поле       | Значение                    |
| ---------- | --------------------------- |
| **Версия** | SERVICE_VERSION_PLACEHOLDER |
| **Статус** | Active                      |
| **Дата**   | 2026-07-08                  |

---

## Схема потока

1. Клиент отправляет запрос с заголовком `Authorization: Bearer <jwt>`
2. `UseAuthentication()` — JWT Bearer Handler проверяет токен (подпись, issuer, audience, lifetime)
3. `ClaimsPrincipal` создаётся из claims токена
4. `UseAuthorization()` — политика (`AdminOnly`, `Operator`, `AuditViewer`) проверяет claims
5. Контроллер обрабатывает запрос или возвращает 403

---

## Политики

| Политика    | Требуемые роли           | Эндпоинты                                                         |
| ----------- | ------------------------ | ----------------------------------------------------------------- |
| Default     | Любая аутентификация     | Все защищённые                                                    |
| AdminOnly   | Admin                    | `PUT /api/v{v}/logging/level`                                     |
| Operator    | Admin, Operator          | (зарезервировано)                                                 |
| AuditViewer | Admin, Operator, Auditor | `GET /api/v{v}/logging/level`, `GET /api/v{v}/logging/categories` |

> 💡 Политики настраиваются в `Security/AuthorizationExtensions.cs`. При необходимости добавить свои роли — расширьте `AuthorizationExtensions.AddCustomAuthorization()`.

---

## Claim types

| Claim          | Значение в JWT                     | Роль в claims                   |
| -------------- | ---------------------------------- | ------------------------------- |
| Name           | `http://.../claims/name`           | имя пользователя                |
| NameIdentifier | `http://.../claims/nameidentifier` | ID пользователя                 |
| Role           | `http://.../claims/role`           | роль (Admin, Operator, Auditor) |

Важно: `RoleClaimType` и `NameClaimType` настроены в `TokenValidationParameters` на длинные URI (`ClaimTypes.Role`, `ClaimTypes.Name`). Это обеспечивает совместимость между `JwtSecurityTokenHandler` (тесты) и `JsonWebTokenHandler` (сервер).

---

## Контроллер LoggingController

**Уровень класса:** `[Authorize]` — требует аутентификации, но не конкретной роли.

**Уровень метода:**

- `PUT /level` — `[Authorize(Policy = "AdminOnly")]` — только Admin
- `GET /level` — `[Authorize(Policy = "AuditViewer")]` — Admin, Operator, Auditor
- `GET /categories` — `[Authorize(Policy = "AuditViewer")]` — Admin, Operator, Auditor

---

## Генератор токенов `/dev/token`

**Статус:** временный эндпоинт, используется на период отладки.

Эндпоинт зарегистрирован только в Development-окружении (внутри `if (app.Environment.IsDevelopment())`). Принимает POST с телом `{ username, roles }` и возвращает валидный JWT.

Короткие claim types (`name`, `sub`, `role`) — совместимость с `JsonWebTokenHandler`. Не совпадает с длинными URI (`ClaimTypes.Role`) в тестовом фабрике, но серверный inbound map маппит автоматически.

**Удалить** после завершения отладки авторизации или перед production-деплоем.

---

## Известный баг (исправлен v1.4.0): AND-логика `[Authorize]`

### Суть

В ASP.NET Core, если `[Authorize]` стоит и на классе, и на методе — политики **комбинируются по AND**. Обе должны пройти.

### Как было

Класс `LoggingController` имел `[Authorize(Policy = "AdminOnly")]` на классе и `[Authorize(Policy = "AuditViewer")]` на методах. Результат: даже AuditViewer-методы требовали роль Admin.

### Почему незаметно

Юнит-тесты контроллера создавали экземпляр напрямую (`new LoggingController(...)`) и вызывали методы — middleware авторизации не запускался. Поэтому тесты проходили.

### Как обнаружили

Интеграционные тесты (через `WebApplicationFactory`) отправляли реальные HTTP-запросы через полный middleware pipeline. Тесты `AuditViewer_WithOperator_Returns200` и `AuditViewer_WithAuditor_Returns200` возвращали 403 вместо 200. При этом `AdminOnly_WithAdmin_Returns200` проходил — потому что Admin проходит обе политики.

### Как исправлено

- На классе: `[Authorize]` (только аутентификация, без policy)
- На методе `SetLevel`: `[Authorize(Policy = "AdminOnly")]`
- На методах `GetLevel` / `GetCategories`: `[Authorize(Policy = "AuditViewer")]` (без изменений)

---
