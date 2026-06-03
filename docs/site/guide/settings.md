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
                DefaultValue = "SimpleModule",
                Type = SettingType.Text,
            })
            .Add(new SettingDefinition
            {
                Key = "app.theme",
                DisplayName = "Theme",
                Description = "Default color theme for the application",
                Group = "Appearance",
                Scope = SettingScope.User,
                DefaultValue = "light",
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
| `DefaultValue` | `string?` | Default value stored as a raw string (e.g. `"90"`, `"true"`, `"SimpleModule"`) -- it does not need to be JSON-encoded |
| `Type` | `SettingType` | Value type -- drives the input control and validation |
| `AllowedValues` | `IReadOnlyList<string>?` | Permitted values for a `Select` setting |
| `Min` / `Max` | `double?` | Inclusive bounds for a `Number` setting |
| `Pattern` | `string?` | Regex applied to `Text` / `Password` / `MultilineText` values |
| `Required` | `bool` | When `true`, an empty value is rejected |
| `Sensitive` | `bool` | Masks the value in API responses (see [Sensitive Settings](#sensitive-settings)) |
| `Order` | `int` | Sort order within a group in the admin UI |
| `Placeholder` | `string?` | Placeholder text for the input control |

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

The type determines the input control rendered in the admin UI and the validation applied when a value is saved:

```csharp
public enum SettingType
{
    Text = 0,          // Single-line text input
    Number = 1,        // Numeric input (honours Min / Max)
    Bool = 2,          // Toggle switch
    Json = 3,          // JSON editor (wide row)
    Select = 4,        // Dropdown bound to AllowedValues
    Color = 5,         // Hex colour picker (#RRGGBB)
    Url = 6,           // Absolute http/https URL
    Email = 7,         // Email address
    Password = 8,      // Masked input; usually paired with Sensitive = true
    MultilineText = 9, // Textarea (wide row)
    DateTime = 10,     // ISO 8601 date-time
}
```

### Field Types and Validation

Each type maps to a dedicated input component on the frontend and a server-side rule in `SettingValidator`. Values are validated both when written through the API and before persistence:

| Type | Renders as | Validation |
|------|-----------|------------|
| `Text` | Text input | `Required`, optional `Pattern` |
| `Number` | Number input | Must parse as a number; honours `Min` / `Max` |
| `Bool` | Toggle switch | Must be `true` / `false` |
| `Json` | JSON editor | Must be valid JSON |
| `Select` | Dropdown | Must be one of `AllowedValues` |
| `Color` | Colour picker | Must match `#RRGGBB` |
| `Url` | URL input | Must be an absolute `http`/`https` URL |
| `Email` | Email input | Must be a valid email address |
| `Password` | Masked input | `Required`, optional `Pattern`; set `Sensitive = true` to mask in responses |
| `MultilineText` | Textarea | `Required`, optional `Pattern` |
| `DateTime` | Date-time input | Must parse as an ISO 8601 date-time |

`Email`, `Url`, `Color`, and `DateTime` apply their own format check and ignore `Pattern` to avoid duplicate errors. When a value fails validation, write operations return **422 Unprocessable Entity** with the errors keyed by setting key (the service throws `SettingValidationException`, which carries the `Key` and a list of `Errors`).

```csharp
// A Select with a fixed set of choices
.Add(new SettingDefinition
{
    Key = "app.theme",
    DisplayName = "Theme",
    Group = "Appearance",
    Scope = SettingScope.User,
    Type = SettingType.Select,
    AllowedValues = ["light", "dark", "system"],
    DefaultValue = "system",
})
// A bounded Number
.Add(new SettingDefinition
{
    Key = "auditlogs.retention.days",
    DisplayName = "Retention Days",
    Group = "Audit Logs",
    Scope = SettingScope.System,
    Type = SettingType.Number,
    Min = 1,
    Max = 3650,
    DefaultValue = "90",
})
// A sensitive secret
.Add(new SettingDefinition
{
    Key = "email.smtp.password",
    DisplayName = "SMTP Password",
    Group = "Email",
    Scope = SettingScope.System,
    Type = SettingType.Password,
    Sensitive = true,
});
```

### Sensitive Settings

Mark secrets with `Sensitive = true`. Their stored value is **never returned** by the list/get endpoints -- the DTO's `Value` comes back as `null` and the UI shows a "set" placeholder rather than the secret. To read the actual value at runtime, use the typed contract (`GetSettingAsync<T>`) or the [resolved endpoint](#resolved-and-decoded-values) from server code; do not round-trip secrets through the browser.

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
    // Read a stored value as a string, or typed
    Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null);

    // Resolve a user setting through the fallback chain (user -> app -> default)
    Task<string?> ResolveUserSettingAsync(string key, string userId);
    Task<JsonElement?> ResolveUserSettingElementAsync(string key, string userId);

    // Write
    Task SetSettingAsync(string key, JsonElement value, SettingScope scope, string? userId = null);
    Task SetManyAsync(IReadOnlyList<BulkSettingUpdate> updates);
    Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null);
    Task ResetToDefaultAsync(string key, SettingScope scope, string? userId = null);

    // DTO-shaped reads (apply masking for sensitive settings)
    Task<IEnumerable<SettingValueDto>> GetSettingValuesAsync(SettingsFilter? filter = null);
    Task<SettingValueDto?> GetSettingValueAsync(string key, SettingScope scope, string? userId = null);
}
```

::: warning Values are `JsonElement`
`SetSettingAsync` takes a `System.Text.Json.JsonElement`, not a string, so values are stored and round-tripped as typed JSON. The DTO reads (`GetSettingValuesAsync` / `GetSettingValueAsync`) return `SettingValueDto`, whose `Value` is `null` for sensitive settings.
:::

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

### Resolved and Decoded Values

`ResolveUserSettingAsync` returns the resolved value as a string; `ResolveUserSettingElementAsync` returns it as a `JsonElement` (the *decoded* value, even for sensitive settings). The latter backs the `GET /api/settings/{key}/resolved` endpoint, which the UI uses to show a user the effective value (their override, or the inherited default) without exposing raw stored secrets in list responses.

```csharp
// Decoded fallback value for the current user
var theme = await settings.ResolveUserSettingElementAsync("app.theme", userId);
```

### Writing Settings

Values are passed as `JsonElement`:

```csharp
using System.Text.Json;

JsonElement Json<T>(T value) => JsonSerializer.SerializeToElement(value);

// Set an application setting
await settings.SetSettingAsync("app.title", Json("My App"), SettingScope.Application);

// Set a user preference
await settings.SetSettingAsync("app.theme", Json("dark"), SettingScope.User, userId);

// Reset to the definition's default (same as deleting the stored value)
await settings.ResetToDefaultAsync("app.theme", SettingScope.User, userId);

// Delete a user override (reverts to application default)
await settings.DeleteSettingAsync("app.theme", SettingScope.User, userId);
```

### Bulk Updates

`SetManyAsync` writes several settings in one call, validating each. User-scoped entries are rejected -- use the per-user endpoints for those.

```csharp
await settings.SetManyAsync(
[
    new BulkSettingUpdate { Key = "app.title", Scope = SettingScope.Application, Value = Json("My App") },
    new BulkSettingUpdate { Key = "auditlogs.retention.days", Scope = SettingScope.System, Value = Json(30) },
]);
```

## HTTP API

The Settings module exposes a REST surface under `/api/settings`. Read endpoints require authentication; write endpoints require the `Settings.Update` permission.

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/settings` | List settings (filterable by scope/group), with sensitive values masked |
| `GET` | `/api/settings/{key}?scope=` | Get a single setting (the `scope` query param is required) |
| `GET` | `/api/settings/{key}/resolved` | Resolved, decoded value for the current user |
| `GET` | `/api/settings/definitions` | All setting definitions |
| `PUT` | `/api/settings` | Update one system/application setting |
| `PUT` | `/api/settings/bulk` | Update many system/application settings |
| `DELETE` | `/api/settings/{key}?scope=` | Reset a setting to its default |
| `GET` | `/api/settings/me` | The current user's settings with resolved fallbacks |
| `PUT` | `/api/settings/me` | Update one of the current user's settings |
| `DELETE` | `/api/settings/me/{key}` | Clear a user override (revert to the app default) |

The single `PUT`/`DELETE` and the bulk endpoint reject `User`-scoped writes with **400 Bad Request** -- user preferences go through the `/me` endpoints.

## Admin and User UI

Two redesigned pages ship with the module:

- **Admin settings** (`/settings/manage`) -- system and application settings grouped and searchable, with per-scope tabs, a **bulk-edit** mode that batches changes into a single save, scope/override badges, and a per-row reset to default.
- **User settings** (`/settings/me`) -- the signed-in user's preferences, showing the effective value with an annotation for whether it is the user's override or an inherited default, plus a reset that clears the override.

Both pages render the input control for each `SettingType` automatically and surface validation errors returned by the API inline.

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

## Next Steps

- [Inertia.js Integration](/guide/inertia) -- how server data reaches React components
- [Frontend Overview](/frontend/overview) -- the complete frontend architecture
- [Configuration Reference](/reference/configuration) -- all framework configuration options
