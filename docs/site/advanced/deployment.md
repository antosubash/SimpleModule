---
outline: deep
---

# Deployment

SimpleModule applications deploy as standard ASP.NET applications. This guide covers Docker deployment, the CI/CD pipeline, and production configuration.

## Docker

### Dockerfile

The project includes a multi-stage Dockerfile that produces a minimal runtime image:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore (layer caching)
COPY Directory.Build.props Directory.Packages.props ./
COPY *.slnx ./
COPY framework/SimpleModule.Core/*.csproj framework/SimpleModule.Core/
COPY framework/SimpleModule.Database/*.csproj framework/SimpleModule.Database/
COPY framework/SimpleModule.Generator/*.csproj framework/SimpleModule.Generator/
COPY framework/SimpleModule.Blazor/*.csproj framework/SimpleModule.Blazor/
COPY template/SimpleModule.Host/*.csproj template/SimpleModule.Host/
# ... module project files ...
RUN dotnet restore template/SimpleModule.Host/SimpleModule.Host.csproj

# Build and publish
COPY . .
RUN dotnet publish template/SimpleModule.Host/SimpleModule.Host.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SimpleModule.Host.dll"]
```

Key points:
- **Multi-stage build** -- the SDK image is only used for building; the final image uses the smaller `aspnet` runtime image
- **Layer caching** -- project files are copied and restored before the full source copy, so NuGet restore is cached across builds
- The runtime container exposes port **8080**

### Docker Compose

For local testing or simple deployments, use `docker-compose.yml`:

```yaml
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Database__DefaultConnection=Host=postgres;Database=simplemodule;Username=simplemodule;Password=simplemodule
    depends_on:
      postgres:
        condition: service_healthy

  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: simplemodule
      POSTGRES_PASSWORD: simplemodule
      POSTGRES_DB: simplemodule
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U simplemodule"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  pgdata:
```

Start with:

```bash
docker compose up -d
```

The API service waits for PostgreSQL to pass its health check before starting.

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on every push to `main` and every pull request. It has four jobs:

### 1. Build

```
build → Restore → Build
```

Verifies the solution compiles without errors.

### 2. Test (SQLite)

```
test-sqlite → Run all tests with in-memory SQLite
```

Runs the full test suite using SQLite (the default provider). This is fast and catches most issues.

### 3. Test (PostgreSQL)

```
test-postgresql → Run all tests against PostgreSQL service container
```

Runs the same test suite against a real PostgreSQL 16 instance. This catches provider-specific issues like SQL dialect differences. The job also verifies that EF Core migrations are up to date:

```bash
dotnet ef migrations has-pending-model-changes
```

The connection string is passed via environment variable:

```
Database__DefaultConnection=Host=localhost;Database=simplemodule_test;Username=test;Password=test
```

### 4. Publish

```
publish → dotnet publish -c Release (only on main branch)
```

Runs only when tests pass on the `main` branch. Produces the release artifacts.

### Pipeline Diagram

```
build ──┬── test-sqlite ────┬── publish (main only)
        └── test-postgresql ─┘
```

The test jobs run in parallel after build succeeds. Publish runs after both test jobs pass.

## Production Build

Build a release-optimized application with:

```bash
dotnet publish template/SimpleModule.Host/SimpleModule.Host.csproj -c Release
```

This produces a self-contained deployment package in the default publish directory.

## Production Database

PostgreSQL is the recommended database for production. Configure it via `appsettings.json` or environment variables:

### Using appsettings.json

```json
{
  "Database": {
    "DefaultConnection": "Host=your-server;Database=simplemodule;Username=app;Password=secret",
    "Provider": "PostgreSQL"
  }
}
```

### Using Environment Variables

Environment variables override `appsettings.json`. Use the `__` (double underscore) separator for nested keys:

```bash
export Database__DefaultConnection="Host=your-server;Database=simplemodule;Username=app;Password=secret"
export Database__Provider="PostgreSQL"
```

::: warning
For production, always use environment variables or a secrets manager for connection strings. Never commit credentials to source control.
:::

## Environment Configuration

### Key Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production`, `Development` |
| `Database__DefaultConnection` | Database connection string | `Host=...;Database=...` |
| `Database__Provider` | Database provider | `Sqlite`, `PostgreSQL` |

### Docker Environment

When running in Docker, pass environment variables via `docker-compose.yml` or `docker run`:

```bash
docker run -d \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Database__DefaultConnection="Host=db;Database=simplemodule;Username=app;Password=secret" \
  simplemodule
```

## Database Initialization

SimpleModule uses `EnsureCreated()` by default, which creates the database schema if it does not exist. For production environments with evolving schemas, use EF Core migrations per module:

```bash
# Add a migration for a specific module's DbContext
dotnet ef migrations add InitialCreate \
  --project modules/Products/src/Products \
  --startup-project template/SimpleModule.Host

# Apply migrations
dotnet ef database update \
  --project modules/Products/src/Products \
  --startup-project template/SimpleModule.Host
```

::: tip
Each module has its own DbContext and schema. In PostgreSQL, modules use separate schemas for isolation. In SQLite, modules use table prefixes.
:::
