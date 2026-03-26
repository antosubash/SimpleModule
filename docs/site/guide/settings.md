---
outline: deep
---

# Settings

SimpleModule provides a settings infrastructure that lets modules declare configurable values with metadata. Settings are stored in a database and scoped to different levels (system, application, user).

## Overview

The settings system has two parts:

1. **Setting definitions** -- declared by modules at startup, describing what settings exist and their metadata
2. **Settings storage** -- the Settings module provides persistence and retrieval through `ISettingsContracts`

## Defining Settings

Override `ConfigureSettings` in your module class to declare settings:

```csharp
[Module("Settings", RoutePrefix = "/settings", ViewPrefix = "/settings")]
public class SettingsModule : IModule
{
    public void ConfigureSettings(ISettingsBuilder settings)
    {
        settings
            .Add(new SettingDefinition
            {
                Key = "app.title",
                DisplayName = "Application Title",
                Group = "General",
                Scope = SettingScope.Application,
                DefaultValue = "\"SimpleModule\"",
                Type = SettingType.Text,
            })
            .Add(new SettingDefinition
            {
                Key = "app.theme",
                DisplayName = "Theme",
                Description = "Default color theme for the application",
                Group = "Appearance",
                Scope = SettingScope.User,
                DefaultValue = "\"light\"",
                Type = SettingType.Text,
            });
    }
}
```

### ISettingsBuilder

The builder collects definitions from all modules:

```csharp
public interface ISettingsBuilder
{
    ISettingsBuilder Add(SettingDefinition definition);
}
```

Calls can be chained since `Add` returns the builder.

### SettingDefinition

Each setting is described by a `SettingDefinition`:

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Unique identifier (convention: `module.name`) |
| `DisplayName` | `string` | Human-readable label for admin UI |
| `Description` | `string?` | Optional help text |
| `Group` | `string?` | Groups related settings in the UI |
| `Scope` | `SettingScope` | Where the setting applies |
| `DefaultValue` | `string?` | Default value (as a string) |
| `Type` | `SettingType` | Value type for UI rendering |

### SettingScope

Settings are scoped to control who can change them and where they apply:

```csharp
public enum SettingScope
{
    System = 0,      // Infrastructure settings (maintenance mode, feature flags)
    Application = 1, // App-wide settings (timezone, title)
    User = 2,        // Per-user preferences (theme, language)
}
```

- **System** -- only administrators can modify; affects the entire system
- **Application** -- app-wide configuration visible to all users
- **User** -- per-user preferences that override application defaults

### SettingType

The type determines how the setting is rendered in the admin UI:

```csharp
public enum SettingType
{
    Text = 0,   // Single-line text input
    Number = 1, // Numeric input
    Bool = 2,   // Toggle switch
    Json = 3,   // JSON editor
}
```

## Real-World Example

The AuditLogs module demonstrates a comprehensive settings setup with multiple related settings:

```csharp
public void ConfigureSettings(ISettingsBuilder settings)
{
    settings
        .Add(new SettingDefinition
        {
            Key = "auditlogs.capture.http",
            DisplayName = "HTTP Request Capture",
            Description = "Capture all HTTP requests in audit log",
            Group = "Audit Logs",
            Scope = SettingScope.System,
            DefaultValue = "true",
            Type = SettingType.Bool,
        })
        .Add(new SettingDefinition
        {
            Key = "auditlogs.retention.days",
            DisplayName = "Retention Days",
            Description = "Number of days to keep audit entries",
            Group = "Audit Logs",
            Scope = SettingScope.System,
            DefaultValue = "90",
            Type = SettingType.Number,
        })
        .Add(new SettingDefinition
        {
            Key = "auditlogs.excluded.paths",
            DisplayName = "Excluded Paths",
            Description = "Comma-separated path prefixes to skip",
            Group = "Audit Logs",
            Scope = SettingScope.System,
            DefaultValue = "/health,/metrics,/_content,/js/,/css/",
            Type = SettingType.Text,
        });
}
```

## Reading and Writing Settings

The Settings module exposes `ISettingsContracts` for other modules to read and write setting values:

```csharp
public interface ISettingsContracts
{
    Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null);
    Task<string?> ResolveUserSettingAsync(string key, string userId);
    Task SetSettingAsync(string key, string value, SettingScope scope, string? userId = null);
    Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<IEnumerable<Setting>> GetSettingsAsync(SettingsFilter? filter = null);
}
```

### Reading Settings

```csharp
public class MyService(ISettingsContracts settings)
{
    public async Task DoWorkAsync()
    {
        // Read a typed setting
        var retentionDays = await settings.GetSettingAsync<int>(
            "auditlogs.retention.days",
            SettingScope.System
        );

        // Read a string setting
        var title = await settings.GetSettingAsync(
            "app.title",
            SettingScope.Application
        );
    }
}
```

### User Setting Resolution

`ResolveUserSettingAsync` implements a **fallback chain**: it first checks for a user-scoped value, then falls back to the application-scoped default:

```csharp
// Returns user's theme if set, otherwise the app default
var theme = await settings.ResolveUserSettingAsync("app.theme", userId);
```

### Writing Settings

```csharp
// Set an application setting
await settings.SetSettingAsync("app.title", "My App", SettingScope.Application);

// Set a user preference
await settings.SetSettingAsync("app.theme", "dark", SettingScope.User, userId);

// Delete a user override (reverts to application default)
await settings.DeleteSettingAsync("app.theme", SettingScope.User, userId);
```

## Settings Definition Registry

The `ISettingsDefinitionRegistry` provides read-only access to all registered setting definitions at runtime:

```csharp
public interface ISettingsDefinitionRegistry
{
    IReadOnlyList<SettingDefinition> GetDefinitions(SettingScope? scope = null);
    SettingDefinition? GetDefinition(string key);
}
```

This is used by the admin UI to dynamically render settings forms:

```csharp
app.MapGet("/api/settings/definitions", (ISettingsDefinitionRegistry registry) =>
{
    // All definitions
    var all = registry.GetDefinitions();

    // Only system-scoped definitions
    var system = registry.GetDefinitions(SettingScope.System);

    // Look up a specific setting
    var def = registry.GetDefinition("app.title");
});
```

## Key Naming Conventions

Follow the `module.category.name` pattern for setting keys:

| Key | Module | Category | Name |
|-----|--------|----------|------|
| `app.title` | app | -- | title |
| `app.theme` | app | -- | theme |
| `auditlogs.capture.http` | auditlogs | capture | http |
| `auditlogs.retention.days` | auditlogs | retention | days |
| `system.maintenance_mode` | system | -- | maintenance_mode |

::: tip
Use dot-separated keys for consistency. The `Group` property on `SettingDefinition` controls visual grouping in the UI independently from the key structure.
:::
