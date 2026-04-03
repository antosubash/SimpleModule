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
| SQLite | Table name prefixes | `Products_Products`, `Orders_Orders` |
| PostgreSQL | Schemas | `products.Products`, `orders.Orders` |

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
        Key = "Products.MaxItemsPerPage",
        DisplayName = "Max Items Per Page",
        Description = "Maximum number of products displayed per page",
        Group = "Products",
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

## AI Provider Configuration

SimpleModule supports multiple AI providers. Configure one in `appsettings.json`:

### Anthropic (Claude)

```json
{
  "AI": {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-sonnet-4-20250514"
    }
  }
}
```

### OpenAI

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4o"
    }
  }
}
```

### Azure OpenAI

```json
{
  "AI": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "DeploymentName": "gpt-4o",
      "ApiKey": "your-key"
    }
  }
}
```

### Ollama (Local)

```json
{
  "AI": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "llama3"
    }
  }
}
```

::: tip
Use environment variables for API keys in production: `AI__Anthropic__ApiKey`, `AI__OpenAI__ApiKey`, etc.
:::

## Agent Configuration

The agent runtime is configured under the `Agents` section:

```json
{
  "Agents": {
    "Enabled": true,
    "MaxTokens": 4096,
    "Temperature": 0.7,
    "EnableRag": true,
    "EnableStreaming": true,
    "SessionTimeout": "00:30:00",
    "RateLimit": {
      "RequestsPerMinute": 60,
      "TokensPerMinute": 100000
    }
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `Agents:Enabled` | `true` | Global kill switch for AI agents |
| `Agents:MaxTokens` | `4096` | Default max tokens per response |
| `Agents:Temperature` | `0.7` | Default sampling temperature |
| `Agents:EnableRag` | `true` | Enable RAG context injection |
| `Agents:EnableStreaming` | `true` | Allow SSE streaming responses |
| `Agents:SessionTimeout` | `00:30:00` | Session inactivity timeout |
| `Agents:RateLimit:RequestsPerMinute` | `60` | Per-user request rate limit |
| `Agents:RateLimit:TokensPerMinute` | `100000` | Per-user token rate limit |

## RAG Configuration

```json
{
  "Rag": {
    "DefaultTopK": 5,
    "MinScore": 0.7,
    "EmbeddingDimension": 1536,
    "IndexOnStartup": true
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `Rag:DefaultTopK` | `5` | Number of results per query |
| `Rag:MinScore` | `0.7` | Minimum similarity score threshold |
| `Rag:EmbeddingDimension` | `1536` | Vector embedding dimension |
| `Rag:IndexOnStartup` | `true` | Index knowledge base on application start |

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

## Next Steps

- [API Reference](/reference/api) -- complete interface and type documentation
- [Deployment](/advanced/deployment) -- production configuration and Docker setup
- [Database](/guide/database) -- database-specific configuration details
