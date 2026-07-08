# Auth Flow — Authentication and Authorization

| Field       | Value                       |
| ----------- | --------------------------- |
| **Version** | SERVICE_VERSION_PLACEHOLDER |
| **Status**  | Active                      |
| **Date**    | 2026-07-08                  |

---

## Flow Diagram

1. Client sends a request with the header `Authorization: Bearer <jwt>`
2. `UseAuthentication()` — JWT Bearer Handler validates the token (signature, issuer, audience, lifetime)
3. `ClaimsPrincipal` is created from the token claims
4. `UseAuthorization()` — policy (`AdminOnly`, `Operator`, `AuditViewer`) checks claims
5. Controller processes the request or returns 403

---

## Policies

| Policy      | Required Roles           | Endpoints                                                         |
| ----------- | ------------------------ | ----------------------------------------------------------------- |
| Default     | Any authentication       | All protected endpoints                                           |
| AdminOnly   | Admin                    | `PUT /api/v{v}/logging/level`                                     |
| Operator    | Admin, Operator          | (reserved)                                                        |
| AuditViewer | Admin, Operator, Auditor | `GET /api/v{v}/logging/level`, `GET /api/v{v}/logging/categories` |

> 💡 Policies are configured in `Security/AuthorizationExtensions.cs`. To add custom roles, extend `AuthorizationExtensions.AddCustomAuthorization()`.

---

## Claim Types

| Claim          | Value in JWT                       | Role in Claims                  |
| -------------- | ---------------------------------- | ------------------------------- |
| Name           | `http://.../claims/name`           | username                        |
| NameIdentifier | `http://.../claims/nameidentifier` | user ID                         |
| Role           | `http://.../claims/role`           | role (Admin, Operator, Auditor) |

Important: `RoleClaimType` and `NameClaimType` are configured in `TokenValidationParameters` with long URIs (`ClaimTypes.Role`, `ClaimTypes.Name`). This ensures compatibility between `JwtSecurityTokenHandler` (tests) and `JsonWebTokenHandler` (server).

---

## LoggingController

**Class level:** `[Authorize]` — requires authentication but not a specific role.

**Method level:**

- `PUT /level` — `[Authorize(Policy = "AdminOnly")]` — Admin only
- `GET /level` — `[Authorize(Policy = "AuditViewer")]` — Admin, Operator, Auditor
- `GET /categories` — `[Authorize(Policy = "AuditViewer")]` — Admin, Operator, Auditor

---

## Token Generator `/dev/token`

**Status:** temporary endpoint, used during debugging period.

The endpoint is registered only in the Development environment (inside `if (app.Environment.IsDevelopment())`). Accepts POST with body `{ username, roles }` and returns a valid JWT.

Short claim types (`name`, `sub`, `role`) — compatibility with `JsonWebTokenHandler`. Does not match the long URIs (`ClaimTypes.Role`) in the test factory, but the server-side inbound map handles the mapping automatically.

**Delete** after authorization debugging is complete or before production deployment.

---

## Known Bug (Fixed in v1.4.0): AND Logic for `[Authorize]`

### Issue

In ASP.NET Core, when `[Authorize]` is placed on both the class and method, policies are **combined with AND logic**. Both must be satisfied.

### What Happened

The `LoggingController` class had `[Authorize(Policy = "AdminOnly")]` on the class and `[Authorize(Policy = "AuditViewer")]` on methods. Result: even AuditViewer methods required the Admin role.

### Why It Went Undetected

Unit tests for the controller created instances directly (`new LoggingController(...)`) and called methods — the authorization middleware was not invoked. Therefore, all tests passed.

### How It Was Discovered

Integration tests (via `WebApplicationFactory`) sent real HTTP requests through the full middleware pipeline. Tests `AuditViewer_WithOperator_Returns200` and `AuditViewer_WithAuditor_Returns200` returned 403 instead of 200. Meanwhile, `AdminOnly_WithAdmin_Returns200` passed — because Admin satisfies both policies.

### How It Was Fixed

- On the class: `[Authorize]` (authentication only, no policy)
- On the `SetLevel` method: `[Authorize(Policy = "AdminOnly")]`
- On `GetLevel` / `GetCategories` methods: `[Authorize(Policy = "AuditViewer")]` (unchanged)

---
