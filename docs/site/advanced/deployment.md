---
outline: deep
---

# Deployment

SimpleModule applications deploy as standard ASP.NET applications. This guide covers Docker deployment, the CI/CD pipeline, and production configuration.

## Docker

### Dockerfile

The project includes a multi-stage Dockerfile that produces a minimal runtime image. The snippet below is a simplified reference — see the repository's actual `Dockerfile` for the full script, which lists every module's `.csproj` and workspace `package.json` explicitly for optimal layer caching:

```dockerfile
# Stage 1: Base — .NET SDK + Node.js 22
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Stage 2: .NET restore (cached unless .csproj / props files change)
FROM base AS restore
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY *.slnx ./
# Copy every framework/module/package .csproj individually for layer caching
# ... (see real Dockerfile for the full list) ...
RUN dotnet restore template/SimpleModule.Host/SimpleModule.Host.csproj

# Stage 3: npm restore (cached unless package.json / lockfile changes)
FROM restore AS npm-restore
WORKDIR /src
COPY package.json package-lock.json ./
# Copy every workspace package.json individually for layer caching
# ... (see real Dockerfile for the full list) ...
RUN npm ci

# Stage 4: Frontend build + .NET publish
FROM npm-restore AS build
WORKDIR /src
COPY . .
RUN npm run build \
    && npx @tailwindcss/cli \
       -i template/SimpleModule.Host/Styles/app.css \
       -o template/SimpleModule.Host/wwwroot/css/app.css \
       --minify
RUN dotnet publish template/SimpleModule.Host/SimpleModule.Host.csproj \
    -c Release -o /app/publish --no-restore \
    -p:ErrorOnDuplicatePublishOutputFiles=false \
    -p:JsBuildCommand=echo

# Stage 5: Runtime (slim image, non-root user)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && apt-get clean && rm -rf /var/lib/apt/lists/* \
    && groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup --create-home appuser
COPY --from=build --chown=appuser:appgroup /app/publish .
RUN mkdir -p /app/data /app/storage && chown appuser:appgroup /app/data /app/storage

# DEPLOY_VERSION feeds Inertia's asset-version header; if empty, the framework
# falls back to the entry assembly's last-write timestamp.
ARG DEPLOY_VERSION=
ENV DEPLOYMENT_VERSION=${DEPLOY_VERSION}

USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "SimpleModule.Host.dll"]
```

Key points:
- **Five stages** -- `base` (SDK + Node 22), `restore` (.NET restore), `npm-restore` (workspace npm ci), `build` (frontend + dotnet publish), `runtime` (slim aspnet image)
- **Non-root runtime** -- creates `appuser` (UID 1001) and runs the container as that user; owns `/app/data` (SQLite) and `/app/storage` (local files)
- **Per-project COPY for layer caching** -- every `.csproj` and workspace `package.json` is copied individually so `dotnet restore` and `npm ci` only re-run when those manifests change
- **`DEPLOY_VERSION` build arg** -- feeds `DEPLOYMENT_VERSION` for Inertia cache-busting; override with `--build-arg DEPLOY_VERSION=$(git rev-parse --short HEAD)` for deterministic versions
- The runtime container exposes port **8080**

### Docker Compose

For local testing or simple deployments, use `docker-compose.yml`. The real compose file defines three services — `api`, `worker`, and `postgres` — so background jobs run in a dedicated consumer process while the web tier stays in producer mode:

```yaml
services:
  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Database__DefaultConnection: "Host=postgres;Port=5432;Database=simplemodule;Username=simplemodule;Password=${POSTGRES_PASSWORD:-simplemodule}"
      Database__Provider: PostgreSQL
      OpenIddict__BaseUrl: ${APP_BASE_URL:-http://localhost:8080}
      # api enqueues jobs but never executes them — the worker does.
      BackgroundJobs__WorkerMode: Producer
    volumes:
      - storage_data:/app/storage
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -sf http://localhost:8080/health/live || exit 1"]
      interval: 30s
      timeout: 5s
      start_period: 15s
      retries: 3
    restart: unless-stopped

  worker:
    build:
      context: .
      dockerfile: Dockerfile.worker
    environment:
      DOTNET_ENVIRONMENT: Development
      Database__DefaultConnection: "Host=postgres;Port=5432;Database=simplemodule;Username=simplemodule;Password=${POSTGRES_PASSWORD:-simplemodule}"
      Database__Provider: PostgreSQL
      BackgroundJobs__WorkerMode: Consumer
    volumes:
      - storage_data:/app/storage
    depends_on:
      postgres:
        condition: service_healthy
    restart: unless-stopped

  postgres:
    image: postgres:17
    environment:
      POSTGRES_USER: simplemodule
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-simplemodule}
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
    restart: unless-stopped

volumes:
  pgdata:
  storage_data:
```

Start with:

```bash
docker compose up -d
```

The API service waits for PostgreSQL to pass its health check before starting. The `storage_data` volume is shared between `api` and `worker` so uploaded files are visible from both the web upload path and the background job path.

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
| `OpenIddict__BaseUrl` | Public base URL used to register OpenIddict redirect URIs | `https://app.example.com` |
| `BackgroundJobs__WorkerMode` | Role in the background-job pipeline | `Producer` (web tier) or `Consumer` (worker tier) |

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

SimpleModule relies on EF Core migrations per module — there is no `EnsureCreated()` bootstrap in the framework. Generate and apply migrations for each module's DbContext:

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

## Next Steps

- [Configuration Reference](/reference/configuration) -- all framework settings in one place
- [API Reference](/reference/api) -- complete interface and type documentation
- [Database](/guide/database) -- module database contexts and migration patterns
