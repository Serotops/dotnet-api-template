# Security Policy

## Reporting a vulnerability

Please **do not** open a public issue for security vulnerabilities.

Instead, report them privately via [GitHub Security Advisories](https://github.com/Serotops/dotnet-api-template/security/advisories/new),
or by email to the maintainer. You can expect an initial response within a few days.

## Scope

This is a project template. The most security-relevant areas are:

- **Secrets management** — `Jwt:SigningKey` and connection strings must come from environment
  variables, user-secrets, or a secret manager. Nothing secret should be committed.
- **Authentication/authorization** — JWT validation in `ApplicationServicesExtensions`.
- **Request logging** — `RequestResponseLoggingMiddleware` redacts known credential/PII fields.
- **Security headers** — `SecurityHeadersMiddleware`.

When you generate a project from this template, review and adjust these for your threat model
before deploying.
