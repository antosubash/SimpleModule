# Missing Cross-Cutting Modules

Analysis of cross-cutting concerns not yet covered by the framework.

## Already Covered

- Exception handling (`GlobalExceptionHandler`)
- Authorization & permissions (`PermissionRegistry`, `IPermissionContracts`)
- Event bus (`IEventBus`)
- Validation (`ValidationBuilder`)
- Settings (`ISettingsContracts`, `SettingDefinitionRegistry`)
- Menu system (`IMenuRegistry`)
- Health checks (`DatabaseHealthCheck`, `/health/live`, `/health/ready`)
- Structured logging + OpenTelemetry
- Database multi-provider & schema isolation
- Authentication (OpenIddict OIDC + ASP.NET Identity)
- Test infrastructure (`SimpleModuleWebApplicationFactory`)
- Compile-time module discovery (source generator)

---

## High Value

### AuditLog

Current audit logging in Admin is manual (`AuditService.LogAsync()`). An AuditLog module would provide automatic change tracking via an EF Core `SaveChanges` interceptor across all module DbContexts. Every module benefits immediately with zero code changes.

- `IAuditContracts` for querying audit history
- EF Core interceptor that captures entity changes (create, update, delete)
- Admin UI for browsing audit trail with filtering
- Per-module opt-in/out via configuration

### BackgroundJobs

No scheduled or deferred work abstraction exists. Modules need cleanup tasks, async processing, and retries.

- `IJobScheduler` contract for scheduling one-off and recurring jobs
- Wraps Hangfire or a lightweight hosted-service scheduler
- Dashboard UI for job monitoring
- Module hook: `ConfigureJobs()` on `IModule`

### Notifications

`ConsoleEmailSender` is the only notification path. A Notifications module would give all modules a unified way to notify users.

- Channels: email, in-app, push
- `INotificationService` contract
- Template system for notification content
- User notification preferences (integrates with Settings)
- Natural event bus consumer (e.g., `OrderCreatedEvent` → email)

### FileStorage

No file/blob abstraction. Products need images, PageBuilder needs media, Users need avatars.

- `IFileStorage` contract with upload/download/delete
- Providers: local filesystem, S3, Azure Blob Storage
- Thumbnail generation
- Admin UI for media library
- Per-module storage isolation

---

## Medium Value

### Localization

Settings has `app.language` but nothing consumes it. A Localization module would enable multi-language support.

- Resource management with key-value translations
- Middleware for culture-aware responses
- Admin UI for managing translations
- Module hook: `ConfigureResources()` on `IModule`

### Caching

No caching abstraction exists. Each module must implement its own caching.

- `IModuleCache<T>` contract with get/set/invalidate
- Backends: in-memory, Redis
- Cache invalidation via event bus integration
- Per-module cache configuration

### FeatureFlags

No way to toggle features per module, user, or role.

- `IFeatureFlagService` contract
- Scopes: global, per-module, per-role, per-user
- Admin UI for flag management
- Module hook: `ConfigureFeatureFlags()` on `IModule`

---

## Nice to Have

### RateLimiting

No throttling or DoS protection.

- Per-module rate limit policies
- Module hook: `ConfigureRateLimits()` on `IModule`
- IP-based and user-based throttling

### Webhooks

External event delivery for third-party integrations.

- Complements the internal event bus
- Webhook registration and management UI
- Retry with exponential backoff
- Signature verification for security

### RealTime

No SignalR/WebSocket support for live updates.

- `IRealtimeHub` contract for broadcasting
- Live notifications, dashboard updates
- Per-module channel isolation

---

## Recommended Priority

1. **AuditLog** — highest leverage, every module benefits automatically
2. **BackgroundJobs** — unlocks async workflows across all modules
3. **FileStorage** — unblocks richer content for Products, PageBuilder, Users
