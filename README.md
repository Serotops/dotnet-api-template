# DotnetApiTemplate

[![CI](https://github.com/Serotops/dotnet-api-template/actions/workflows/ci.yml/badge.svg)](https://github.com/Serotops/dotnet-api-template/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A starting point for ASP.NET Core APIs built with clean architecture. It comes with a working
sample resource (`Car`), JWT auth, validation, structured logging, health checks, rate limiting,
Docker, and a test suite, so you can delete the sample and build your own thing instead of wiring
the plumbing from scratch.

Target framework is .NET 10. The database is PostgreSQL through EF Core (Npgsql).

## What's in the box

- Clean architecture split across five projects (Domain, Application, Persistence, Infrastructure, API)
- EF Core with a generic repository, a sample entity, and an audit interceptor
- Request validation with FluentValidation, surfaced through a custom action filter
- The Result pattern (FluentResults) for expected failures, exceptions for the rest
- JWT bearer authentication, with a clearly-marked demo token endpoint for local use
- Per-IP rate limiting, configurable security headers, and CORS driven by config
- Liveness and readiness health checks
- Serilog logging (console everywhere, rolling file in development)
- Swagger UI with API versioning, available in development
- A multi-stage Dockerfile that runs as a non-root user
- xUnit tests (unit and integration), Central Package Management, analyzers as errors
- GitHub Actions CI that also proves the template still scaffolds

## Layout

```
src/
  DotnetApiTemplate.Domain          Entities, enums. No dependencies on anything else.
  DotnetApiTemplate.Application     Services, DTOs, validators, interfaces.
  DotnetApiTemplate.Persistence     DbContext, repositories, EF configs, interceptors, migrations.
  DotnetApiTemplate.Infrastructure  Cross-cutting integrations (email, external APIs, ...).
  DotnetApiTemplate.API             Controllers, middleware, DI wiring, entry point.

tests/
  DotnetApiTemplate.UnitTests
  DotnetApiTemplate.IntegrationTests
```

Dependencies point inward. API depends on Application, Application depends on Domain. Persistence
and Infrastructure depend on Domain and Application but never on each other, and Domain depends on
nothing. If you find yourself wanting Persistence to reference Infrastructure (or vice versa), put
the shared abstraction in Application instead.

## Running it locally

You need the .NET 10 SDK and Docker.

```bash
cp .env.example .env              # optional, the defaults already work
docker compose up -d              # PostgreSQL, published on localhost:54320
dotnet run --project src/DotnetApiTemplate.API
```

The API listens on `https://localhost:7001` and `http://localhost:5001` (see
`Properties/launchSettings.json`). Swagger UI is at `/api`, and only in development.

In development the database schema is created for you on startup, because
`Database:MigrateOnStartup` is `true` in `appsettings.Development.json`. Production is a different
story, covered under [Migrations](#migrations).

## Using it as a template

The project is registered as a `dotnet new` template, which is the cleaner option because the
template engine rewrites namespaces, project names, folders, the solution file, Docker config, and
the database name in one shot.

```bash
git clone https://github.com/Serotops/dotnet-api-template.git
dotnet new install ./dotnet-api-template

dotnet new dotnet-api -n MyProject -o ./MyProject

cd MyProject
docker compose up -d
dotnet run --project src/MyProject.API
```

Each scaffold also gets a fresh `UserSecretsId`, so two projects generated from the template don't
end up sharing the same local secret store.

If you'd rather not use the template engine, clone the repo and find-and-replace
`DotnetApiTemplate` with your project name across the files, folders, and the `.sln`.

## How requests flow

Middleware runs in this order (see `Program.cs`):

1. HSTS, only outside development.
2. CORS.
3. Security headers (`SecurityHeadersMiddleware`).
4. Exception handling (`ExceptionHandlingMiddleware`).
5. Request/response logging (`RequestResponseLoggingMiddleware`).
6. Rate limiter.
7. Swagger, only in development.
8. Authentication, then authorization.

Successful responses return the DTO directly. Failures are wrapped in `ApiResponse<T>` with an
error code, a list of messages, and the correlation id, so clients get a predictable error shape
without it leaking into the happy path.

### Error handling

There are two layers, and they don't overlap:

- Expected business failures (not found, a broken business rule, a validation problem) travel up
  as a `Result` from the service layer. The controller turns a failed result into the right status
  code.
- Anything unexpected throws, and `ExceptionHandlingMiddleware` catches it, logs it with the
  correlation id, and returns a 500-class response. It also checks `Response.HasStarted` first: if
  the response has already begun streaming there's no way to write a clean error body, so it
  rethrows instead of masking the original exception.

### Validation

Validators live in the Application layer and are registered from the assembly. A custom
`ValidationFilter` runs them before the action executes, so controllers never see invalid input.
Failures come back with field-level messages and error codes.

### Auditing

`AuditableEntity` carries `CreatedAt`, `ModifiedAt`, `CreatedBy`, and `ModifiedBy`. An EF Core
`SaveChangesInterceptor` fills them in on every save, reading the current user from
`ICurrentUserService`. You don't set these by hand.

### Repositories

`Repository<T>` covers the usual CRUD. Entity-specific repositories inherit from it and add their
own queries (the sample `CarRepository` does filtering, sorting, and pagination). Every method
takes a `CancellationToken`, and it's threaded all the way from the controller down to EF Core, so
a cancelled request actually stops work instead of running to completion.

## Authentication

JWT bearer auth is configured from the `Jwt` section. The mutating endpoints on `CarsController`
(`POST`, `PUT`, `PATCH`, `DELETE`) require `[Authorize]`; the read endpoints are open. Swagger has
an Authorize button so you can paste a token and try the protected routes.

The signing key is a secret, so it's left empty in `appsettings.json`. A throwaway key sits in
`appsettings.Development.json` to keep local runs frictionless. Outside the Testing environment the
app refuses to start if the key is missing or shorter than 32 bytes (HS256 needs at least 256
bits), which turns a vague runtime failure into a clear startup error. Supply a real key out of
band:

```bash
# local development
dotnet user-secrets set "Jwt:SigningKey" "<a-long-random-secret>" --project src/DotnetApiTemplate.API

# or an environment variable (the double underscore maps to the config section separator)
export Jwt__SigningKey="<a-long-random-secret>"
```

Don't commit a real key.

### The demo token endpoint

`AuthController` exposes `POST /api/v1/auth/token`, which hands back a signed JWT for whatever
username you send, with no password check. It exists so the protected endpoints are usable the
minute you clone the repo, and nothing more.

It's fenced off so it can't follow you into production:

- The route returns 404 unless the app is in development and `Auth:EnableDemoTokenEndpoint` is
  `true`. That flag is on in `appsettings.Development.json` and off in `appsettings.json`.
- If the flag is ever switched on outside development, the app throws at startup. A misconfigured
  deploy crashes loudly instead of quietly shipping an auth bypass.

```bash
curl -X POST https://localhost:7001/api/v1/auth/token \
  -H "Content-Type: application/json" -d '{"username":"demo"}'
```

Before you ship anything real, replace this with an actual identity flow: verify credentials
against a user store, attach roles and claims, and decide whether you need refresh tokens.

## Rate limiting

A single global limiter is registered through `Microsoft.AspNetCore.RateLimiting`. It's a
fixed-window limiter partitioned by client IP, and over-limit requests get a 429. The numbers come
from the `RateLimiting` section:

```jsonc
"RateLimiting": {
  "PermitLimit": 100,     // requests allowed per window, per IP
  "WindowSeconds": 10,
  "QueueLimit": 0         // 0 means reject immediately instead of queueing
}
```

One thing to watch: the partition key is `RemoteIpAddress`. Behind a reverse proxy or ingress
that's the proxy's address, so every client collapses into one bucket and you're rate limiting the
whole world together. If you deploy behind a proxy, configure `ForwardedHeaders` so
`RemoteIpAddress` reflects the real client. When you outgrow one global limit, swap in named
per-endpoint policies.

## CORS

Allowed origins come from `Cors:AllowedOrigins`. When the list is empty (the default in
`appsettings.json`) the policy falls back to `AllowAnyOrigin`, which is convenient locally and
wrong in production. Set explicit origins before you deploy. Development already lists
`http://localhost:3000` and `http://localhost:5173` for a typical SPA dev server.

## Health checks

Two endpoints, split deliberately:

- `GET /health/live` runs no checks. It answers as long as the process is up. This is what an
  orchestrator should use for liveness, because if liveness checked the database a brief outage
  would get your healthy pods killed and restarted for no reason.
- `GET /health/ready` includes the database connectivity check (`AddDbContextCheck`, tagged
  `ready`). Use it for readiness and load-balancer gating.

The Docker `HEALTHCHECK` hits `/health/live`.

## API versioning

Versions can be supplied three ways: URL segment (`/api/v1/...`), a header (`x-api-version`), or a
media type parameter. The default is `1.0` when nothing is specified. Swagger shows one document
per discovered version.

## Logging

Serilog writes structured logs to the console in every environment, which is what you want in a
container where the platform collects stdout. In development it adds a rolling file sink under
`../logs/` keeping seven days. There's no file sink in production, partly because it's noise the
log driver already handles and partly because the container runs as a non-root user that can't
write there anyway.

## Configuration

### Connection string

Development (`appsettings.Development.json`) points at the Docker database on your host:

```
Host=localhost;Port=54320;Database=DotnetApiTemplateDb;User ID=postgres;Password=root
```

The base `appsettings.json` uses `Host=db;Port=5432`, which is the service name on the compose
network, for container-to-container access.

### Environment variables

`.env` (copied from `.env.example`) feeds the PostgreSQL container in `docker-compose.yml`:
`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`. The compose file has sane defaults, so a
missing `.env` won't stop the database from starting.

The API can override the connection string credentials at runtime with `DB_USER` and
`DB_PASSWORD`, which is handy when the username and password come from a secret store rather than
the connection string itself.

`.env` is gitignored. Only `.env.example` is committed.

### Dependencies and build settings

Package versions are pinned in one place, `Directory.Packages.props`, using Central Package
Management. Individual project files list packages without version numbers. Shared build settings
(nullable, implicit usings, .NET analyzers, warnings-as-errors) live in `Directory.Build.props`;
the test projects import it and then relax warnings-as-errors so test tooling noise doesn't fail
the build. Dependabot watches NuGet, the GitHub Actions, and the Docker base image.

## Migrations

In development they run on startup. In production, don't do that. Migrating from app startup races
when more than one instance boots at once, forces the runtime database user to hold DDL
permissions it otherwise shouldn't, and couples your boot time to schema changes. Run them as their
own deploy step instead. `Database:MigrateOnStartup` is the switch, and it's `false` in
`appsettings.json`.

The plain command, if your deploy runner has the SDK:

```bash
dotnet ef database update \
  --project src/DotnetApiTemplate.Persistence \
  --startup-project src/DotnetApiTemplate.API
```

A self-contained bundle is usually the better fit for a pipeline, because the runner doesn't need
the SDK or the source, just the bundle and a connection string:

```bash
dotnet ef migrations bundle \
  --project src/DotnetApiTemplate.Persistence \
  --startup-project src/DotnetApiTemplate.API \
  --self-contained -r linux-x64 -o efbundle

./efbundle --connection "Host=...;Database=...;Username=...;Password=..."
```

If a DBA applies changes, hand them an idempotent SQL script that's safe to run more than once:

```bash
dotnet ef migrations script --idempotent \
  --project src/DotnetApiTemplate.Persistence \
  --startup-project src/DotnetApiTemplate.API \
  -o migrate.sql
```

If you're running a single instance and accept the trade-offs, you can set
`Database__MigrateOnStartup=true` and keep the startup behaviour. The template doesn't force the
choice on you.

## Adding an entity

Using the bundled `Car` as the worked example:

1. Add the entity under `Domain/Entities/`, inheriting `AuditableEntity`.
2. Add a `DbSet<>` to `DotnetApiTemplateDbContext`.
3. Add an EF configuration under `Persistence/Configurations/`.
4. Declare a repository interface in `Application/Interfaces/Repositories/`.
5. Implement it in `Persistence/Repositories/` (inherit `Repository<T>` for the CRUD).
6. Add DTOs in `Application/DTOs/` and validators in `Application/Validators/`.
7. Add a service interface and implementation in `Application/`.
8. Add a controller in `API/Controllers/`.
9. Register the new service and repository in `ApplicationServicesExtensions.cs`.
10. Create the migration:

```bash
dotnet ef migrations add AddYourEntity \
  --project src/DotnetApiTemplate.Persistence \
  --startup-project src/DotnetApiTemplate.API
```

## Docker

For local development you only need the database:

```bash
docker compose up -d        # PostgreSQL on localhost:54320
```

The Dockerfile builds the API into a small Alpine image, runs it as the non-root user that the
.NET base image provides, and ships a healthcheck against `/health/live`:

```bash
docker build -f src/DotnetApiTemplate.API/Dockerfile -t myapp .

docker run -p 8080:8080 \
  -e Jwt__SigningKey="<a-long-random-secret>" \
  -e ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..." \
  myapp
```

Two reminders the container will enforce for you: it won't start without a valid `Jwt__SigningKey`,
and it won't migrate on boot unless you pass `Database__MigrateOnStartup=true`.

## Tests

```bash
dotnet test                                          # everything
dotnet test tests/DotnetApiTemplate.UnitTests        # unit only
dotnet test tests/DotnetApiTemplate.IntegrationTests # integration only
```

Unit tests cover the service and validator logic with xUnit, FluentAssertions, and Moq.
Integration tests spin up the API with `WebApplicationFactory` against an in-memory EF provider, so
they need nothing external. Authentication is stubbed there with a test handler, and the rate
limiter is given a huge permit limit so a growing suite never trips a 429.

## Continuous integration

`.github/workflows/ci.yml` runs on pushes and pull requests to `main`, in two jobs:

- Build and test in Release.
- Install the template, scaffold a fresh project from it, and build that. This catches the case
  where the app still compiles but the template itself is broken, which a normal build wouldn't
  notice.

`.github/workflows/codeql.yml` runs CodeQL on the same triggers plus a weekly schedule.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Report security issues privately, as described in
[SECURITY.md](SECURITY.md).

## License

MIT. See [LICENSE](LICENSE).
