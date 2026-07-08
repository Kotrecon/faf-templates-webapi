# API — Current State

| Field       | Value                       |
| ----------- | --------------------------- |
| **Version** | SERVICE_VERSION_PLACEHOLDER |
| **Status**  | Active                      |
| **Date**    | 2026-07-08                  |
| **API**     | v1 (URL-based versioning)   |

> This document describes the public and internal API endpoints of the service, their authentication, request/response formats, and error codes.
>
> Health check implementation details — see [`operability.md`](operability.md).

---

## General Principles

### API Versioning

**Strategy:** URL-based versioning via `Asp.Versioning.Mvc` 10.0.0.

**Format:** `/api/v{version}/[controller]`

**Current version:** 1.0

**Compatibility policy:**

| Change                               | Type  | Version |
| ------------------------------------ | ----- | ------- |
| Adding a new endpoint                | Minor | v1.x    |
| Adding an optional field to response | Minor | v1.x    |
| Changing a required field            | Major | v2.0    |
| Removing an endpoint                 | Major | v2.0    |
| Changing a field type                | Major | v2.0    |

**Supported versions:**

| Version | Status | Release Date | EOL |
| ------- | ------ | ------------ | --- |
| v1.0    | Active | 2026-07-08   | —   |

**Response headers:**

```http
api-supported-versions: 1.0
api-deprecated-versions:
```

### Authentication

**API (port 8080):**
All endpoints (except dev, metadata, and OpenAPI) require a JWT Bearer token in the header:

```bash
Authorization: Bearer <token>
```

**Health Checks (port 8081):**
No authentication required. Protection via network isolation (port is not routed externally).

**Dev API (port 8080, Development only):**
No authentication required. Used for generating test JWT tokens.

**Metadata API (port 8080):**
No authentication required. Public endpoint with API metadata.

### Correlation

The `X-Correlation-Id` header is automatically generated (`Guid.CreateVersion7`) or passed through from the incoming request. Available in logs and OTel traces.

### OpenAPI UI

In the Development environment, an interactive UI is available for testing the API:

| URL                                     | Description                           |
| --------------------------------------- | ------------------------------------- |
| `http://localhost:8080/scalar/v1`       | Scalar UI — interactive documentation |
| `http://localhost:8080/openapi/v1.json` | OpenAPI 3.1 document                  |

**Stack:** `Microsoft.AspNetCore.OpenApi` 10.0.9 + `Scalar.AspNetCore` 2.13.19

JWT authorization is embedded in the OpenAPI document via `DocumentTransformer` — the **Authorize** button in the UI.

**Configuration:**

- Title, Version, Description are taken from `ApiMetadataOptions` (appsettings.json)
- Contact info — developer contact from configuration
- Security scheme: Bearer JWT, automatically applied to all endpoints

---

## Endpoints

### API (port 8080)

| Method | Route                        | Description                       | Access      |
| ------ | ---------------------------- | --------------------------------- | ----------- |
| GET    | `/api/v1/logging/level`      | Current logging levels            | AuditViewer |
| PUT    | `/api/v1/logging/level`      | Change logging level              | AdminOnly   |
| GET    | `/api/v1/logging/categories` | List of categories with overrides | AuditViewer |
| GET    | `/api/metadata`              | API metadata                      | Anonymous   |

### Dev API (port 8080, Development only)

| Method | Route        | Description             | Access    |
| ------ | ------------ | ----------------------- | --------- |
| POST   | `/dev/token` | Generate test JWT token | Anonymous |

### Health Checks (port 8081 — internal)

| Method | Route           | Description             | Access   |
| ------ | --------------- | ----------------------- | -------- |
| GET    | `/health/live`  | Process is alive        | Internal |
| GET    | `/health/ready` | Ready to accept traffic | Internal |
| GET    | `/health`       | Aggregated response     | Internal |

---

## Error Format (RFC 7807 ProblemDetails)

All business errors are returned in **RFC 7807 ProblemDetails** format via the Result Pattern.

### Response Structure

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

### Error Types

| Error Type        | HTTP Code | When Used                                  |
| ----------------- | --------- | ------------------------------------------ |
| ValidationError   | 422       | Input data validation errors               |
| NotFoundError     | 404       | Resource not found                         |
| ConflictError     | 409       | Conflict (duplicate, uniqueness violation) |
| ForbiddenError    | 403       | Access denied                              |
| BusinessRuleError | 400       | Business rule violation                    |

### Technical Errors

Unhandled exceptions are caught by `ExceptionHandlerMiddleware` and returned in a simplified format:

```json
{
  "error": {
    "code": 500,
    "message": "An unexpected error occurred"
  }
}
```

Internal details (stack trace, IP, paths) are **never** returned to the client — only logged.

---

## Dev API

### POST /dev/token

Generates a test JWT token. **Development environment only.**

**Request:**

```json
{
  "username": "admin",
  "roles": ["Admin"]
}
```

| Field    | Type     | Required | Description                   |
| -------- | -------- | -------- | ----------------------------- |
| username | string   | Yes      | Username (claim `name`)       |
| roles    | string[] | Yes      | Array of roles (claim `role`) |

**Allowed roles:** `Admin`, `Operator`, `Auditor`

**Response (200 OK):**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires": 8
}
```

| Field   | Type   | Description                   |
| ------- | ------ | ----------------------------- |
| token   | string | JWT token (HS256, 8-hour TTL) |
| expires | int    | Expiration in hours           |

**Token retrieval example (PowerShell):**

```powershell
$token = (Invoke-RestMethod -Method POST `
  -Uri http://localhost:8080/dev/token `
  -ContentType "application/json" `
  -Body '{"username":"admin","roles":["Admin"]}').token
```

---

## Metadata API

### GET /api/metadata

Public endpoint — API metadata is not a secret. No authentication required.

**Response (200 OK):**

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

| Field       | Type   | Description                |
| ----------- | ------ | -------------------------- |
| title       | string | API title                  |
| version     | string | API version (semver)       |
| description | string | API description            |
| developer   | object | Developer contact info     |
| name        | string | Developer name             |
| email       | string | Developer email (optional) |
| url         | string | Developer URL (optional)   |

**Configuration:**

Values are taken from `ApiMetadataOptions` in `appsettings.json`:

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

**Caching:** 1 hour (`ResponseCache(Duration = 3600)`)

**Response codes:**

| Code | Description |
| ---- | ----------- |
| 200  | Success     |

---

## Logging API

### GET /api/v1/logging/level

Returns current logging levels.

**Response (200 OK):**

```json
{
  "default": "Information",
  "overrides": {
    "Microsoft": "Warning",
    "System": "Warning"
  }
}
```

| Code | Description                         |
| ---- | ----------------------------------- |
| 200  | Success                             |
| 401  | Not authenticated                   |
| 403  | Missing Admin/Operator/Auditor role |

### PUT /api/v1/logging/level

Changes the logging level for a category or root level.

**Request:**

```json
{
  "category": "Microsoft.AspNetCore",
  "level": "Warning"
}
```

| Field    | Type   | Required | Description                  |
| -------- | ------ | -------- | ---------------------------- |
| category | string | No       | Category (null = root level) |
| level    | string | Yes      | Logging level                |

**Allowed levels:** Debug, Information, Warning, Error  
**Forbidden levels:** Fatal, Verbose (security)

**Responses:**

- `200 OK` — level changed (empty body)

- `400 Bad Request` — invalid level (ProblemDetails):

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

- `404 Not Found` — category not found (ProblemDetails):

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

- `422 Unprocessable Entity` — validation error (ProblemDetails):

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

| Code | Description        |
| ---- | ------------------ |
| 200  | Level changed      |
| 400  | Invalid request    |
| 401  | Not authenticated  |
| 403  | Missing Admin role |
| 404  | Category not found |
| 422  | Validation error   |

### GET /api/v1/logging/categories

Returns a list of categories with overrides.

**Response (200 OK):**

```json
["Microsoft", "System"]
```

| Code | Description                         |
| ---- | ----------------------------------- |
| 200  | Success                             |
| 401  | Not authenticated                   |
| 403  | Missing Admin/Operator/Auditor role |

---

## Health Checks API

> Implementation details (what each endpoint checks, graceful shutdown, rate limiting) — see [`operability.md`](operability.md).

### Request

All endpoints accept GET requests without a body:

```bash
GET /health/live
GET /health/ready
GET /health
```

### Responses

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

### Response Codes

| Code | Description                             |
| ---- | --------------------------------------- |
| 200  | Process alive / dependencies ready      |
| 429  | Rate limit exceeded (30 req/10s)        |
| 503  | Process hung / dependencies unavailable |

---

## Error Codes (General)

| Code | Description                   |
| ---- | ----------------------------- |
| 400  | Invalid request               |
| 401  | Not authenticated             |
| 403  | No permissions (policy-based) |
| 404  | Resource not found            |
| 422  | Validation error              |
| 429  | Rate limit exceeded           |
| 503  | Service unavailable (health)  |

---

## Access Policy (Role Matrix)

| Role     | AuditViewer (read) | AdminOnly (write) |
| -------- | :----------------: | :---------------: |
| Admin    |         ✅         |        ✅         |
| Operator |         ✅         |        ❌         |
| Auditor  |         ✅         |        ❌         |

---
