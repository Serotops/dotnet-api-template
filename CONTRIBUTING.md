# Contributing

Thanks for your interest in improving this template!

## Getting started

```bash
git clone https://github.com/Serotops/dotnet-api-template.git
cd dotnet-api-template
docker compose up -d            # start PostgreSQL
dotnet build
dotnet test
```

## Ground rules

- **Build clean.** `dotnet build` must produce 0 warnings (the solution treats warnings as errors).
- **Tests pass.** Run `dotnet test` before opening a PR. Add tests for new behavior.
- **Keep the architecture.** Dependencies flow inward: `API → Application → Domain`. `Persistence` and `Infrastructure` depend on `Domain`/`Application`, never on each other.
- **Conventional-ish commits.** Short imperative subject (e.g. `Add rate limiting`), details in the body if needed.

## Pull requests

1. Fork and branch from `main`.
2. Make your change with tests.
3. Ensure `dotnet build` and `dotnet test` are green — CI runs both on every PR.
4. Open the PR using the template and describe the change and motivation.

## Reporting bugs / requesting features

Use the issue templates. Include the .NET SDK version (`dotnet --info`) and repro steps for bugs.
