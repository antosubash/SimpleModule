---
outline: deep
---

# Configuration

SimpleModule uses the standard ASP.NET configuration system. Settings are loaded from `appsettings.json`, environment-specific files, and environment variables.

## appsettings.json Structure

The default `appsettings.json` for the host application:

```json
{
  "Database": {
    "DefaultConnection": "Data Source=app.db",
    "Provider": "Sqlite"
  },
  "Storage": {
    "Provider": "Local",
    "Local": {
      "BasePath": "./storage"
    }
  },
  "Localization": {
    "DefaultLocale": "en"
  },
  "Passkeys": {
    "ServerDomain": "yourdomain.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Database Configuration

### Provider

The `Database:Provider` setting determines which database engine to use:

| Value | Database | Connection String Format |
|-------|----------|-------------------------|
| `Sqlite` | SQLite | `Data Source=app.db` |
| `PostgreSQL` | PostgreSQL | `Host=server;Database=db;Username=user;Password=pass` |

### Connection String

The `Database:DefaultConnection` setting holds the connection string for the selected provider.

**SQLite (default for development):**

```json
{
  "Database": {
    "DefaultConnection": "Data Source=app.db",
    "Provider": "Sqlite"
  }
}
```

**PostgreSQL (recommended for production):**

```json
{
  "Database": {
    "DefaultConnection": "Host=localhost;Database=simplemodule;Username=app;Password=secret",
    "Provider": "PostgreSQL"
  }
}
```

### Schema Isolation

Each module gets its own isolated storage space:

| Provider | Isolation Strategy | Example |
|----------|-------------------|---------|
| SQLite | Table name prefixes | `Customers_Customers`, `Users_Users` |
| PostgreSQL | Schemas | `customers.Customers`, `users.Users` |

This is configured automatically via `ModuleDbContextInfo` -- you do not need to set this manually.

## Environment Variables

Environment variables override `appsettings.json` values. Use double underscores (`__`) as separators for nested keys:

```bash
# Database configuration
export Database__DefaultConnection="Host=server;Database=simplemodule;Username=app;Password=secret"
export Database__Provider="PostgreSQL"

# ASP.NET environment
export ASPNETCORE_ENVIRONMENT="Production"

# Logging
export Logging__LogLevel__Default="Warning"
```

::: tip
Environment variables take precedence over all JSON configuration files. This is the recommended way to configure secrets in production.
:::

### Common Environment Variables

| Variable | Description | Values |
|----------|-------------|--------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment name | `Development`, `Production` |
| `Database__DefaultConnection` | Database connection string | Provider-specific |
| `Database__Provider` | Database provider | `Sqlite`, `PostgreSQL` |
| `Logging__LogLevel__Default` | Default log level | `Trace`, `Debug`, `Information`, `Warning`, `Error` |

## Development Configuration

The `appsettings.Development.json` file adds development-specific overrides:

```json
{
  "Database": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    },
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "TimestampFormat": "HH:mm:ss.fff ",
        "UseUtcTimestamp": false
      }
    }
  }
}
```

Key differences from production:
- **SQLite** is used by default (no external database needed)
- **EF Core SQL logging** is enabled (`Microsoft.EntityFrameworkCore.Database.Command: Information`) so you can see the generated SQL queries
- **Console formatter** uses a simple format with local timestamps for readability

## Production Configuration

For production deployments, configure via environment variables or a secrets manager:

### Docker Compose

```yaml
services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Database__DefaultConnection=Host=postgres;Database=simplemodule;Username=app;Password=secret
      - OpenIddict__BaseUrl=https://app.example.com
      - BackgroundJobs__WorkerMode=Producer
```

### Docker Run

```bash
docker run -d \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Database__DefaultConnection="Host=db;Database=simplemodule;Username=app;Password=secret" \
  simplemodule
```

### CI/CD

In the CI pipeline, the PostgreSQL test job sets the connection string as an environment variable:

```yaml
env:
  Database__DefaultConnection: "Host=localhost;Database=simplemodule_test;Username=test;Password=test"
```

::: warning
Never commit production credentials to `appsettings.json` or any file in source control. Use environment variables, Docker secrets, or a vault service.
:::

## Module-Specific Settings

Modules can define their own settings through the `ISettingsBuilder` interface in their `ConfigureSettings` method. These settings are registered with a `SettingDefinition`:

```csharp
public void ConfigureSettings(ISettingsBuilder settings)
{
    settings.Add(new SettingDefinition
    {
        Key = "Customers.MaxItemsPerPage",
        DisplayName = "Max Items Per Page",
        Description = "Maximum number of customers displayed per page",
        Group = "Customers",
        DefaultValue = "25",
        Type = SettingType.Integer,
        Scope = SettingScope.Application,
    });
}
```

Module settings are stored in the database and can be managed through the admin UI at runtime without restarting the application.

## Configuration Precedence

ASP.NET loads configuration from multiple sources. Later sources override earlier ones:

1. `appsettings.json` (lowest priority)
2. `appsettings.{Environment}.json`
3. Environment variables
4. Command-line arguments (highest priority)

For the `Development` environment, the effective configuration merges `appsettings.json` with `appsettings.Development.json`, with the Development file winning on conflicts.

## File Storage Configuration

### Local Storage

```json
{
  "Storage": {
    "Provider": "Local",
    "Local": {
      "BasePath": "./storage"
    }
  }
}
```

### AWS S3

```json
{
  "Storage": {
    "Provider": "S3",
    "S3": {
      "BucketName": "my-bucket",
      "AccessKey": "your-access-key",
      "SecretKey": "your-secret-key",
      "Region": "us-east-1",
      "ServiceUrl": "",
      "ForcePathStyle": false
    }
  }
}
```

### Azure Blob Storage

```json
{
  "Storage": {
    "Provider": "Azure",
    "Azure": {
      "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...",
      "ContainerName": "files"
    }
  }
}
```

::: warning
Never commit storage credentials to source control. Use environment variables: `Storage__S3__AccessKey`, `Storage__Azure__ConnectionString`, etc.
:::

## Localization Configuration

```json
{
  "Localization": {
    "DefaultLocale": "en"
  }
}
```

The default locale is used when no user setting or Accept-Language header matches a supported locale.

## OpenIddict Configuration

```json
{
  "OpenIddict": {
    "BaseUrl": "https://app.example.com",
    "AllowPasswordGrant": false
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `OpenIddict:BaseUrl` | _(empty)_ | Public base URL used when registering redirect URIs. Set to your deployed URL (e.g. `https://app.example.com`) so OAuth flows round-trip correctly behind a reverse proxy. |
| `OpenIddict:AllowPasswordGrant` | `false` | Enables the OAuth 2.0 Resource Owner Password Credentials (ROPC) grant. Development-only — `appsettings.Development.json` turns this on so load/integration tests can obtain bearer tokens without a browser. |

## Passkeys Configuration

```json
{
  "Passkeys": {
    "ServerDomain": "yourdomain.com"
  }
}
```

The `Passkeys:ServerDomain` is the WebAuthn relying-party ID. Use your production host name (e.g. `app.example.com`) in production and `localhost` in development.

## Background Jobs Configuration

```json
{
  "BackgroundJobs": {
    "WorkerMode": "Producer"
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `BackgroundJobs:WorkerMode` | `Producer` | Role of the current process in the job pipeline. `Producer` enqueues jobs but does not execute them; `Consumer` dequeues and executes them. In the reference Docker Compose setup the `api` service runs as `Producer` and the `worker` service runs as `Consumer`. |

## Next Steps

- [API Reference](/reference/api) -- complete interface and type documentation
- [Deployment](/advanced/deployment) -- production configuration and Docker setup
- [Database](/guide/database) -- database-specific configuration details
