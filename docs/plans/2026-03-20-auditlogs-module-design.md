# AuditLogs Module Design

## Overview

A comprehensive audit logging module that automatically captures all system activity across three streams — HTTP requests, domain events, and EF Core entity changes — with zero configuration required from other modules.

## Capture Streams

### 1. HTTP Middleware (`AuditMiddleware`)
- Captures every HTTP request: method, path, query string, status code, duration
- User context: user ID, username, IP address, user agent
- Request body capture for POST/PUT/DELETE (with sensitive field redaction)
- Path exclusion list to skip health checks, static assets, etc.
- Runs early in the pipeline, records timing around `next()`

### 2. EventBus Decorator (`AuditingEventBus`)
- Wraps the real `EventBus`, intercepts all `PublishAsync<T>()` calls
- Convention-based extraction from event type name:
  - `{Entity}{Action}Event` → Module, EntityType, Action
  - Properties ending in `Id` → EntityType + EntityId
  - Remaining properties → serialized as Metadata JSON
- Recognized actions: Created, Updated, Deleted, Viewed, Exported, LoginSuccess, LoginFailed, PermissionGranted, PermissionRevoked, SettingChanged
- Unrecognized → `AuditAction.Other` with full event name

### 3. SaveChanges Interceptor (`AuditSaveChangesInterceptor`)
- EF Core `SaveChangesInterceptor` registered globally via `AddModuleDbContext<T>()`
- Before save, snapshots ChangeTracker entries:
  - Added → `Action.Created`, all property values captured
  - Modified → `Action.Updated`, old/new values for changed properties only
  - Deleted → `Action.Deleted`, entity state before deletion
- Module name derived from DbContext type (e.g., `ProductsDbContext` → `Products`)
- Entity type and ID from EF metadata

### Correlation

All three streams share a `CorrelationId` (GUID) set per-request via scoped `IAuditContext`. This links "POST /api/products → 201" with "ProductCreatedEvent" and "Product entity inserted" into a single auditable action.

## Performance

- **Zero request latency impact**: All streams enqueue to an in-memory `Channel<AuditEntry>`
- **Background batch writer** (`AuditWriterService`): `IHostedService` that drains the channel and batch-inserts (flush every 100 entries or 2 seconds)
- **Settings cached**: Read via `ISettingsContracts` backed by `IMemoryCache`
- **Early bailout**: Middleware checks path exclusion list before any work
- **Separate DbContext scope** per batch to prevent memory leaks

## Data Model

### AuditEntry (entity)
| Field | Type | Description |
|-------|------|-------------|
| Id | AuditEntryId (Vogen) | Strongly-typed int ID |
| CorrelationId | Guid | Links related entries |
| Source | AuditSource enum | Http, Domain, ChangeTracker |
| Timestamp | DateTimeOffset | When the entry was created |
| UserId | string? | Authenticated user ID |
| UserName | string? | Authenticated username |
| IpAddress | string? | Client IP |
| UserAgent | string? | Browser/client user agent |
| HttpMethod | string? | GET, POST, etc. |
| Path | string? | Request path |
| QueryString | string? | Query parameters |
| StatusCode | int? | HTTP response status |
| DurationMs | long? | Request duration |
| RequestBody | string? | Redacted JSON body |
| Module | string? | Source module name |
| EntityType | string? | Entity class name |
| EntityId | string? | Entity primary key |
| Action | AuditAction? | Created, Updated, Deleted, etc. |
| Changes | string? | JSON array of {field, old, new} |
| Metadata | string? | Free-form JSON context |

### AuditSource enum
- Http
- Domain
- ChangeTracker

### AuditAction enum
- Created, Updated, Deleted, Viewed
- LoginSuccess, LoginFailed
- PermissionGranted, PermissionRevoked
- SettingChanged, Exported, Other

### Database Indexes
- `(Timestamp DESC)` — retention cleanup, default sort
- `(UserId, Timestamp DESC)` — user activity queries
- `(Module, Timestamp DESC)` — module-scoped queries
- `(CorrelationId)` — correlation lookups
- `(EntityType, EntityId)` — entity history

## Settings (via ISettingsBuilder)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| auditlogs.capture.http | bool | true | Enable HTTP request capture |
| auditlogs.capture.domain | bool | true | Enable domain event capture |
| auditlogs.capture.changes | bool | true | Enable SaveChanges interception |
| auditlogs.capture.requestbodies | bool | true | Capture request bodies |
| auditlogs.capture.querystrings | bool | true | Capture query strings |
| auditlogs.capture.useragent | bool | false | Capture user agent strings |
| auditlogs.retention.enabled | bool | true | Enable automatic cleanup |
| auditlogs.retention.days | int | 90 | Days before purge |
| auditlogs.excluded.paths | string | /health,/metrics,/_content | Paths to skip |

## Contracts Interface

```csharp
public interface IAuditLogContracts
{
    Task<PagedResult<AuditEntry>> QueryAsync(AuditQueryRequest request);
    Task<AuditEntry?> GetByIdAsync(AuditEntryId id);
    Task<IReadOnlyList<AuditEntry>> GetByCorrelationIdAsync(Guid correlationId);
    Task<Stream> ExportAsync(AuditExportRequest request);
    Task<AuditStats> GetStatsAsync(DateTimeOffset from, DateTimeOffset to);
    Task WriteBatchAsync(IReadOnlyList<AuditEntry> entries);
    Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff);
}

public interface IAuditContext
{
    Guid CorrelationId { get; }
    string? UserId { get; set; }
    string? UserName { get; set; }
    string? IpAddress { get; set; }
}
```

## Sensitive Field Redaction

`SensitiveFieldRedactor` strips known fields from request bodies before storage:
- Fields matching: password, secret, token, key, authorization, credential, ssn, credit_card (case-insensitive)
- Replaced with `"[REDACTED]"`
- Applied to JSON request bodies only

## Retention

`AuditRetentionService` — `IHostedService` running on a timer (daily):
- Reads `auditlogs.retention.enabled` and `auditlogs.retention.days` from settings
- Deletes entries where `Timestamp < now - retentionDays`
- Uses batch deletes via `Timestamp` index for efficiency

## Frontend

### Browse Page (AuditLogs/Browse)
- DataGrid with sortable columns: Timestamp, User, Action, Module, Path, Status, Duration
- Filter panel: date range, Module, Action, Source, Status Code dropdowns, text search
- Row click expands to show correlated entries
- Export button (CSV/JSON) with current filters
- Admin sidebar menu item, gated by `AuditLogs.View` permission

### Detail Page (AuditLogs/Detail)
- Full entry data in structured layout
- Request body displayed as syntax-highlighted redacted JSON
- Changes shown as diff table (field, old value, new value)
- Correlated entries listed below

## Module Structure

```
modules/AuditLogs/
├── src/AuditLogs.Contracts/
│   ├── AuditLogs.Contracts.csproj
│   ├── IAuditLogContracts.cs
│   ├── IAuditContext.cs
│   ├── AuditEntry.cs
│   ├── AuditEntryId.cs
│   ├── AuditSource.cs
│   ├── AuditAction.cs
│   ├── AuditQueryRequest.cs
│   ├── AuditExportRequest.cs
│   └── AuditStats.cs
├── src/AuditLogs/
│   ├── AuditLogsModule.cs
│   ├── AuditLogsDbContext.cs
│   ├── AuditLogService.cs
│   ├── AuditContext.cs
│   ├── AuditLogsConstants.cs
│   ├── AuditLogsPermissions.cs
│   ├── Middleware/
│   │   └── AuditMiddleware.cs
│   ├── Pipeline/
│   │   ├── AuditChannel.cs
│   │   ├── AuditWriterService.cs
│   │   └── AuditingEventBus.cs
│   ├── Interceptors/
│   │   └── AuditSaveChangesInterceptor.cs
│   ├── Enrichment/
│   │   └── SensitiveFieldRedactor.cs
│   ├── Retention/
│   │   └── AuditRetentionService.cs
│   ├── Endpoints/AuditLogs/
│   │   ├── GetAllEndpoint.cs
│   │   ├── GetByIdEndpoint.cs
│   │   ├── ExportEndpoint.cs
│   │   └── GetStatsEndpoint.cs
│   ├── EntityConfigurations/
│   │   └── AuditEntryConfiguration.cs
│   ├── Pages/index.ts
│   ├── Views/
│   │   ├── Browse.tsx
│   │   └── Detail.tsx
│   ├── types.ts
│   ├── vite.config.ts
│   └── package.json
└── tests/AuditLogs.Tests/
    ├── AuditLogServiceTests.cs
    ├── AuditMiddlewareTests.cs
    ├── AuditSaveChangesInterceptorTests.cs
    ├── SensitiveFieldRedactorTests.cs
    ├── AuditRetentionServiceTests.cs
    ├── AuditingEventBusTests.cs
    └── AuditLogsEndpointTests.cs
```

## Core Changes

- Add `PagedResult<T>` to `SimpleModule.Core` (with `[Dto]` attribute)
- Register `AuditSaveChangesInterceptor` in `AddModuleDbContext<T>()` extension method
- `AuditSource` enum updated to include `ChangeTracker`
