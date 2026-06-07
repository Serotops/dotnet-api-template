# DotnetApiTemplate

[![CI](https://github.com/Serotops/dotnet-api-template/actions/workflows/ci.yml/badge.svg)](https://github.com/Serotops/dotnet-api-template/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A clean architecture ASP.NET Core API template. Clone it, rename it, and start building.

## Tech Stack

- **.NET 10** / ASP.NET Core
- **PostgreSQL** with EF Core (Npgsql)
- **Serilog** for structured logging
- **FluentValidation** for request validation
- **FluentResults** for the Result pattern
- **Swagger / OpenAPI** with API versioning
- **xUnit** with FluentAssertions for testing
- **Docker** for containerization

## Architecture

```
src/
  DotnetApiTemplate.Domain           # Entities, enums, exceptions -- no dependencies
  DotnetApiTemplate.Application      # Services, DTOs, validators, interfaces
  DotnetApiTemplate.Persistence      # DbContext, repositories, EF configs, interceptors
  DotnetApiTemplate.Infrastructure   # External services (auth, email, etc.)
  DotnetApiTemplate.API              # Controllers, middlewares, startup

tests/
  DotnetApiTemplate.UnitTests
  DotnetApiTemplate.IntegrationTests
```

Dependencies flow inward: API -> Application -> Domain. Persistence and Infrastructure depend on Domain and Application, never on each other.

## Quick Start

```bash
# 1. Copy the example environment file (optional - defaults work out of the box)
cp .env.example .env

# 2. Start the database
docker compose up -d

# 3. Run the API
dotnet run --project src/DotnetApiTemplate.API
```

The API will be available at `https://localhost:7001` (or the port in `launchSettings.json`).
Swagger UI is at `/api` (Development environment only).

In Development, migrations run automatically on startup (`Database:MigrateOnStartup` is `true`
in `appsettings.Development.json`). See [Database migrations](#database-migrations) for production.

## Use as a Template

### Option 1: dotnet new (recommended)

```bash
# Clone and install the template
git clone https://github.com/Serotops/dotnet-api-template.git
dotnet new install ./dotnet-api-template

# Create a new project
dotnet new dotnet-api -n MyProject -o ./MyProject

# Start working
cd MyProject
docker compose up -d
dotnet run --project src/MyProject.API
```

The template engine automatically renames all namespaces, projects, folders, Docker config, and database names.

### Option 2: Manual clone

Clone the repo, then do a find-and-replace of `DotnetApiTemplate` with your project name across all files, folders, and the `.sln` file.

## Project Features

### Middleware Pipeline

Requests flow through these middlewares in order:

1. **Security Headers** -- X-Frame-Options, CSP, Permissions-Policy, etc.
2. **Exception Handling** -- catches unhandled exceptions, returns a structured `ApiResponse<T>` error
3. **Request/Response Logging** -- logs HTTP traffic with correlation IDs; request bodies are logged with sensitive fields (passwords, tokens, etc.) redacted

Successful responses are returned as-is (the DTO directly). Errors are returned wrapped in `ApiResponse<T>` with an error code, message list, and trace id.

### Repository Pattern

A generic `Repository<T>` provides CRUD operations. Entity-specific repositories extend it for custom queries (filtering, sorting, pagination). All methods accept a `CancellationToken` that flows from the controller down to EF Core.

### Auditing

`AuditableEntity` base class with `CreatedAt`, `ModifiedAt`, `CreatedBy`, `ModifiedBy` fields. An EF Core `SaveChangesInterceptor` populates these automatically using the authenticated user from `ICurrentUserService`.

### Validation

FluentValidation validators are registered in DI and executed via a custom `ValidationFilter`. Validation errors return structured responses with error codes.

### Error Handling

Two complementary patterns:
- **Result pattern** (FluentResults) for expected business failures
- **Exceptions** for unexpected errors, caught by the exception middleware

### Authentication

JWT Bearer authentication is configured from the `Jwt` section in `appsettings.json`. Endpoints that mutate data (`POST`, `PUT`, `PATCH`, `DELETE` on `CarsController`) are protected with `[Authorize]`; read endpoints are public. Swagger includes an "Authorize" button to send a Bearer token.

The `Jwt:SigningKey` is a **secret** and is intentionally empty in `appsettings.json`. A throwaway dev key lives in `appsettings.Development.json` so the template runs out of the box. The app **fails fast on startup** outside the Testing environment if the key is missing or shorter than 32 bytes — supply a real one via environment variable, user-secrets, or a secret manager:

```bash
# user-secrets (recommended for local dev)
dotnet user-secrets set "Jwt:SigningKey" "<a-long-random-secret>" --project src/DotnetApiTemplate.API

# or environment variable (note the double underscore)
export Jwt__SigningKey="<a-long-random-secret>"
```

Never commit a production signing key.

**Getting a token (demo):** `AuthController` exposes `POST /api/v1/auth/token` which issues a
signed JWT for any username **without checking credentials** — it exists only so you can try the
`[Authorize]` endpoints immediately. It is gated for safety:

- The route returns **404** unless the app runs in **Development** *and*
  `Auth:EnableDemoTokenEndpoint` is `true` (the default in `appsettings.Development.json`; `false`
  in `appsettings.json`).
- The app **refuses to start** if the flag is enabled outside Development, so a misconfigured
  production deploy fails loudly rather than exposing an auth bypass.

Replace it with a real identity flow (validate credentials, add roles/claims, issue refresh
tokens) before production.

```bash
curl -X POST https://localhost:7001/api/v1/auth/token \
  -H "Content-Type: application/json" -d '{"username":"demo"}'
```

### Rate Limiting

A global fixed-window rate limiter (partitioned by client IP) is enabled via
`Microsoft.AspNetCore.RateLimiting`. Defaults live under the `RateLimiting` config section
(`PermitLimit`, `WindowSeconds`, `QueueLimit`); over-limit requests get `429 Too Many Requests`.
Swap in per-endpoint policies as the API grows.

### CORS

Allowed origins are read from `Cors:AllowedOrigins` in configuration. If the list is empty (the default in `appsettings.json`), the policy falls back to `AllowAnyOrigin` for convenience -- set explicit origins in production.

### API Versioning

Supports URL segment, header (`x-api-version`), and media type versioning. Default is v1.0.

### Health Checks

Two endpoints, suitable for container orchestrators:

- `GET /health/live` — **liveness**: is the process up? No dependency checks, so a transient
  database blip won't cause an orchestrator to kill an otherwise-healthy instance.
- `GET /health/ready` — **readiness**: can it serve traffic? Includes the database connectivity
  check (`AddDbContextCheck`, tagged `ready`).

The Docker `HEALTHCHECK` targets `/health/live`.

## Configuration

### Connection String

Development uses `appsettings.Development.json` pointing to the Docker PostgreSQL instance:

```
Host=localhost;Port=54320;Database=DotnetApiTemplateDb;User ID=postgres;Password=root
```

Production config in `appsettings.json` uses `Host=db;Port=5432` for Docker-to-Docker networking.

### Environment Variables

The `.env` file (copied from `.env.example`) configures the Docker PostgreSQL container:
- `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`

These override the API connection string values at runtime:
- `DB_USER` -- PostgreSQL username
- `DB_PASSWORD` -- PostgreSQL password

`.env` is gitignored; commit only `.env.example`.

### Logging

Serilog logs to the **console** in all environments (structured, container-friendly). In
**Development** an additional rolling **file** sink writes to `../logs/` (7 days retained).
Production stays console-only so logs are collected by your platform's log driver.

### Database migrations

Migrations apply on startup only when `Database:MigrateOnStartup` is `true` — enabled in
`appsettings.Development.json`, disabled by default in `appsettings.json`. For production,
prefer running migrations as a separate, controlled deploy step rather than on app boot
(it avoids races across instances and keeps DDL rights out of the runtime user):

```bash
dotnet ef database update \
  --project src/DotnetApiTemplate.Persistence \
  --startup-project src/DotnetApiTemplate.API
```

### Dependencies & versions

Package versions are managed centrally in `Directory.Packages.props` (Central Package
Management). Shared build settings — nullable, analyzers, warnings-as-errors — live in
`Directory.Build.props`. Dependabot keeps NuGet, GitHub Actions, and the Docker base image
up to date.

## Adding a New Entity

1. Create the entity in `Domain/Entities/` extending `AuditableEntity`
2. Add a `DbSet` in `DotnetApiTemplateDbContext`
3. Add an EF configuration in `Persistence/Configurations/`
4. Create a repository interface in `Application/Interfaces/Repositories/`
5. Implement the repository in `Persistence/Repositories/`
6. Create DTOs in `Application/DTOs/` and validators in `Application/Validators/`
7. Create a service interface and implementation in `Application/`
8. Add a controller in `API/Controllers/`
9. Register the new services in `ApplicationServicesExtensions.cs`
10. Create a migration: `dotnet ef migrations add <Name> --project src/DotnetApiTemplate.Persistence --startup-project src/DotnetApiTemplate.API`

## Docker

### Development

```bash
docker compose up -d   # PostgreSQL on port 54320
```

### Deployment

The Dockerfile produces a minimal Alpine-based image that **runs as a non-root user** and
ships a `HEALTHCHECK` hitting `/health/live`:

```bash
docker build -f src/DotnetApiTemplate.API/Dockerfile -t myapp .
docker run -p 8080:8080 \
  -e Jwt__SigningKey="<a-long-random-secret>" \
  -e ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..." \
  myapp
```

Remember the app **fails fast** without a valid `Jwt__SigningKey`, and migrations don't run on
boot unless `Database__MigrateOnStartup=true`.

## Testing

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/DotnetApiTemplate.UnitTests

# Integration tests only
dotnet test tests/DotnetApiTemplate.IntegrationTests
```

Integration tests use an in-memory database and `WebApplicationFactory` -- no external dependencies needed.

## Contributing

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md). For security issues, see
[SECURITY.md](SECURITY.md).

## License

Licensed under the [MIT License](LICENSE).
