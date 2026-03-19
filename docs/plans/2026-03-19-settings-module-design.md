# Settings Module Design

## Overview

A centralized Settings module that manages user, system, and application-wide settings using a key-value store with JSON-serialized values. Other modules register their own settings via `ConfigureSettings` on `IModule`, following the same pattern as menus.

## Decisions

| Decision | Choice |
|----------|--------|
| Storage | Key-value store (key, JSON value, scope) |
| Defaults | Cascade: User > Application > Code Default |
| UI | Full UI — admin pages + user settings page |
| Permissions | Role-based: admins see all, users see own |
| Extensibility | Modules register settings via `ConfigureSettings(ISettingsBuilder)` on `IModule` |
| Discovery | Source generator creates `CollectModuleSettings()`, mirrors menu pattern |

## Data Model

Single `Settings` table:

| Column | Type | Description |
|--------|------|-------------|
| `Id` | `int` (PK, auto) | Primary key |
| `Key` | `string` | Setting key, e.g. `theme`, `smtp.host`, `products.items_per_page` |
| `Value` | `string?` | JSON-serialized value |
| `Scope` | `enum` (System=0, Application=1, User=2) | Setting scope |
| `UserId` | `string?` | Only populated for User-scoped settings |
| `UpdatedAt` | `DateTimeOffset` | Last modified timestamp |

**Unique constraint**: (`Key`, `Scope`, `UserId`)

**Resolution cascade** (when reading a setting for a user):
1. Check User scope (with UserId) — if found, return it
2. Check Application scope — if found, return it
3. Return code-defined default from the `SettingDefinition`

## Core Contracts (in SimpleModule.Core)

### Types

```csharp
public enum SettingScope { System = 0, Application = 1, User = 2 }

public enum SettingType { String = 0, Number = 1, Boolean = 2, Json = 3 }

public class SettingDefinition
{
    public string Key { get; set; }
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    public SettingScope Scope { get; set; }
    public string? DefaultValue { get; set; }
    public SettingType Type { get; set; }
}
```

### Builder and Registry (in Core)

```csharp
public interface ISettingsBuilder
{
    ISettingsBuilder Add(SettingDefinition definition);
}

public sealed class SettingsBuilder : ISettingsBuilder
{
    private readonly List<SettingDefinition> _definitions = [];

    public ISettingsBuilder Add(SettingDefinition definition)
    {
        _definitions.Add(definition);
        return this;
    }

    public List<SettingDefinition> ToList() => [.. _definitions];
}

public interface ISettingsDefinitionRegistry
{
    IReadOnlyList<SettingDefinition> GetDefinitions(SettingScope? scope = null);
    SettingDefinition? GetDefinition(string key);
}
```

### IModule Extension

```csharp
public interface IModule
{
    virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
    virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
    virtual void ConfigureMenu(IMenuBuilder menus) { }
    virtual void ConfigurePermissions(PermissionRegistryBuilder builder) { }
    virtual void ConfigureSettings(ISettingsBuilder settings) { }  // NEW
}
```

### Settings Contracts (in Settings.Contracts)

```csharp
public interface ISettingsContracts
{
    Task<string?> GetSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<T?> GetSettingAsync<T>(string key, SettingScope scope, string? userId = null);
    Task<string?> ResolveUserSettingAsync(string key, string userId);
    Task SetSettingAsync(string key, string value, SettingScope scope, string? userId = null);
    Task DeleteSettingAsync(string key, SettingScope scope, string? userId = null);
    Task<IEnumerable<SettingDefinition>> GetDefinitionsAsync(SettingScope? scope = null);
}
```

## Module Structure

```
modules/Settings/
├── src/
│   ├── Settings.Contracts/
│   │   ├── Settings.Contracts.csproj
│   │   ├── ISettingsContracts.cs
│   │   ├── Setting.cs                        [Dto]
│   │   ├── UpdateSettingRequest.cs            [Dto]
│   │   └── SettingsFilter.cs                  [Dto]
│   └── Settings/
│       ├── Settings.csproj
│       ├── SettingsModule.cs
│       ├── SettingsDbContext.cs
│       ├── SettingsConstants.cs
│       ├── SettingsService.cs
│       ├── EntityConfigurations/
│       │   └── SettingEntityConfiguration.cs
│       ├── Entities/
│       │   └── SettingEntity.cs
│       ├── Endpoints/
│       │   ├── Settings/
│       │   │   ├── GetSettingsEndpoint.cs
│       │   │   ├── GetSettingEndpoint.cs
│       │   │   ├── UpdateSettingEndpoint.cs
│       │   │   ├── DeleteSettingEndpoint.cs
│       │   │   └── GetDefinitionsEndpoint.cs
│       │   └── UserSettings/
│       │       ├── GetMySettingsEndpoint.cs
│       │       └── UpdateMySettingEndpoint.cs
│       ├── Views/
│       │   ├── AdminSettingsEndpoint.cs
│       │   └── UserSettingsEndpoint.cs
│       ├── Pages/
│       │   └── index.ts
│       ├── package.json
│       └── vite.config.ts
└── tests/
    └── Settings.Tests/
        └── Settings.Tests.csproj
```

## API Routes

- `GET /api/settings?scope=&group=` — list settings (admin)
- `GET /api/settings/{key}?scope=` — get single setting (admin)
- `PUT /api/settings` — upsert setting (admin)
- `DELETE /api/settings/{key}?scope=` — reset to default (admin)
- `GET /api/settings/definitions?scope=` — list registered definitions
- `GET /api/settings/me` — current user's resolved settings
- `PUT /api/settings/me` — update current user setting

## View Routes

- `/settings` — admin settings page (system + application tabs)
- `/settings/me` — user preferences page

## Frontend UI

### Admin Settings Page (`/settings`)

- Tabbed layout: System tab, Application tab
- Settings grouped by `Group` field
- Type-based input rendering: toggle (Boolean), text (String), number (Number), JSON editor (Json)
- Save per-setting or per-group

### User Settings Page (`/settings/me`)

- All settings where `Scope` includes `User`
- Grouped by `Group` field
- Shows current value with indicator for inherited vs overridden
- "Reset to default" action per setting
- Same type-based rendering as admin page

### Shared Components

- `SettingField` — renders input based on `SettingType`
- `SettingGroup` — collapsible card grouping settings by `Group`
- Uses `@simplemodule/ui` components (Radix UI + Tailwind)

## Source Generator Changes

Update the Roslyn source generator to:
1. Detect `ConfigureSettings` overrides on `IModule` implementations
2. Generate `CollectModuleSettings()` extension method (mirrors `CollectModuleMenuItems()`)
3. Called during startup to populate `ISettingsDefinitionRegistry`

## Testing Strategy

### Unit Tests
- `SettingsService` cascade resolution: user > app > code default
- `SettingsBuilder` definition collection and duplicate key detection
- `SettingsDefinitionRegistry` filtering by scope

### Integration Tests
- CRUD endpoints for system/app settings (admin role required)
- User settings endpoints (`/api/settings/me`) — authenticated users only
- Permission enforcement — regular user cannot access admin endpoints
- Cascade resolution end-to-end: set app default, set user override, verify resolution, delete override, verify fallback

### UI Tests (Playwright)
- Admin settings page: navigate tabs, edit a setting, save, verify persisted
- Admin settings page: grouped display renders correctly per module
- User settings page: override a setting, verify "Reset to default" restores inherited value
- User settings page: type-specific inputs render correctly (toggle, text, number)
- Permission enforcement: regular user cannot access admin settings page
- Cross-page: admin changes app default, user settings page reflects new default

### Test Data
- Bogus fakers for `Setting`, `UpdateSettingRequest`, `SettingsFilter`
