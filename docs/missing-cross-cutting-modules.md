# Missing Cross-Cutting Modules

Snapshot of cross-cutting concerns and where each one stands. Everything under "Already Covered" ships in the box; the remaining sections are an open backlog.

## Already Covered

- Exception handling (`GlobalExceptionHandler`)
- Authorization & permissions (`PermissionRegistry`, `IPermissionContracts`)
- Event bus with durable inbox/outbox (Wolverine `IMessageBus`, EF Core persistence)
- Real-time push (`/hub/broadcast` SignalR hub + `@simplemodule/echo` client) — see [Broadcasting](./site/guide/broadcasting.md)
- Validation (FluentValidation + `ValidationResultExtensions`)
- Settings (`ISettingsContracts`, `SettingDefinitionRegistry`)
- Menu system (`IMenuRegistry`)
- Health checks (`DatabaseHealthCheck`, `/health/live`, `/health/ready`)
- Structured logging + OpenTelemetry
- Database multi-provider & schema isolation, soft-delete recovery
- Authentication (OpenIddict OIDC + ASP.NET Identity, active sessions, sign-out everywhere, phone confirmation, self-service unlock)
- Audit log (automatic EF Core change tracking via `AuditLogs` module)
- Background jobs (`BackgroundJobs` module)
- Notifications (`INotifier`, mail / SMS / database channels — see [Notifications](./site/guide/notifications.md))
- File storage (local / S3 / Azure providers + signed-URL generator)
- Localization (`Localization` module, culture middleware, translations admin UI)
- Feature flags (`FeatureFlags` module)
- Rate limiting (DB-defined policies, per-endpoint `.RateLimit(...)`, admin UI — see [Rate Limiting](./site/guide/rate-limiting.md))
- Test infrastructure (`SimpleModuleWebApplicationFactory`)
- Compile-time module discovery (source generator)

---

## Open Backlog

### Caching

No first-class caching abstraction yet. Modules that need caching wire `IMemoryCache` themselves.

- `IModuleCache<T>` contract with get/set/invalidate
- Backends: in-memory, Redis
- Cache invalidation via event bus integration
- Per-module cache configuration

### Webhooks

External event delivery for third-party integrations. Complements the internal event bus by fanning selected events out over HTTP.

- Webhook registration and management UI
- Retry with exponential backoff (already supportable via Wolverine + BackgroundJobs)
- HMAC signature verification for security

---

## Recommended Priority

1. **Caching** — high leverage, several modules (Settings, Permissions, Menus) would benefit immediately.
2. **Webhooks** — unlocks external integrations once a single tenant goes multi-system.
