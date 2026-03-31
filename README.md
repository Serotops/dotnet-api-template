# DotnetApiTemplate

A clean architecture ASP.NET Core API template. Clone it, rename it, and start building.

## Tech Stack

- **.NET 10** / ASP.NET Core
- **PostgreSQL** with EF Core (Npgsql)
- **Serilog** for structured logging
- **FluentValidation** for request validation
- **FluentResults** for the Result pattern
- **AutoMapper** for DTO mapping
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
# 1. Start the database
docker-compose up -d

# 2. Run the API
dotnet run --project src/DotnetApiTemplate.API
```

The API will be available at `https://localhost:5001` (or the port in `launchSettings.json`).
Swagger UI is at `/api`.

Database migrations run automatically on startup.

## Use as a Template

### Option 1: dotnet new (recommended)

```bash
# Install the template
dotnet new install ./DotnetApiTemplate

# Create a new project
dotnet new dotnet-api -n MyProject -o ./MyProject

# Start working
cd MyProject
docker-compose up -d
dotnet run --project src/MyProject.API
```

The template engine automatically renames all namespaces, projects, folders, Docker config, and database names.

### Option 2: Manual clone

Clone the repo, then do a find-and-replace of `DotnetApiTemplate` with your project name across all files, folders, and the `.sln` file.

## Project Features

### Middleware Pipeline

Requests flow through these middlewares in order:

1. **Security Headers** -- X-Frame-Options, CSP, HSTS, etc.
2. **Exception Handling** -- catches unhandled exceptions, returns structured error responses
3. **Request/Response Logging** -- logs HTTP traffic with correlation IDs
4. **Response Wrapper** -- wraps all responses in a standard `ApiResponse<T>` envelope

### Repository Pattern

A generic `Repository<T>` provides CRUD operations. Entity-specific repositories extend it for custom queries (filtering, sorting, pagination).

### Auditing

`AuditableEntity` base class with `CreatedAt`, `ModifiedAt`, `CreatedBy`, `ModifiedBy` fields. An EF Core `SaveChangesInterceptor` populates these automatically using the authenticated user from `ICurrentUserService`.

### Validation

FluentValidation validators are registered in DI and executed via a custom `ValidationFilter`. Validation errors return structured responses with error codes.

### Error Handling

Two complementary patterns:
- **Result pattern** (FluentResults) for expected business failures
- **Exceptions** for unexpected errors, caught by the exception middleware

### API Versioning

Supports URL segment, header (`x-api-version`), and media type versioning. Default is v1.0.

### Health Checks

`/health` endpoint with database connectivity check.

## Configuration

### Connection String

Development uses `appsettings.Development.json` pointing to the Docker PostgreSQL instance:

```
Host=localhost;Port=54320;Database=DotnetApiTemplateDb;User ID=postgres;Password=root
```

Production config in `appsettings.json` uses `Host=db;Port=5432` for Docker-to-Docker networking.

### Environment Variables

The following override connection string values:
- `DB_USER` -- PostgreSQL username
- `DB_PASSWORD` -- PostgreSQL password

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
docker-compose up -d   # PostgreSQL on port 54320
```

### Deployment

The Dockerfile produces a minimal Alpine-based image:

```bash
docker build -f src/DotnetApiTemplate.API/Dockerfile -t myapp .
docker run -p 8080:8080 myapp
```

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
