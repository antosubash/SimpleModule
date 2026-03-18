# Phase 1: Permission Hardening - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase makes the SimpleModule permission system deny-by-default and replaces the admin blanket bypass with explicit, granular permissions. After this phase, every endpoint requires authentication unless explicitly marked `[AllowAnonymous]`, and admin users have specific permissions that can be audited and revoked.

</domain>

<decisions>
## Implementation Decisions

### Fallback Authorization Policy
- Use ASP.NET Core's built-in `FallbackPolicy` in authorization configuration to deny unauthenticated requests by default
- Audit all endpoints via grep + manual review to identify which need `[AllowAnonymous]` — safest approach to avoid breaking public endpoints
- Configure the fallback policy in `Program.cs` authorization config — standard ASP.NET Core pattern, co-located with other auth setup
- Keep current generator behavior — it already applies `.RequireAuthorization()` on auto-registered groups; fallback policy handles the rest

### Admin Permission Granularity
- Remove admin role bypass entirely — admins should have explicit permissions for granular control and auditability
- Seed all defined permissions to the Admin role at startup — admins get everything by default but it's explicit and removable per-permission
- No "super admin" concept — all admins get the same permission set; avoids role hierarchy complexity
- Assign permissions to Admin role first, then remove the bypass — prevents lockout during transition (critical sequencing from research)

### E2E Test Compatibility
- Update the test auth scheme to respect the same authorization pipeline — catches real auth bugs
- Update Playwright test helpers to include permission claims — mirrors real user behavior
- Add a smoke test that hits a protected endpoint without auth and expects 401 — validates the core security change

### Claude's Discretion
No items deferred to Claude's discretion — all decisions captured above.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `PermissionAuthorizationHandler` at `framework/SimpleModule.Core/Authorization/PermissionAuthorizationHandler.cs` — currently has admin bypass on line 13
- `EndpointPermissionExtensions.RequirePermission()` — already calls `RequireAuthorization()` with `PermissionRequirement`
- `RequirePermissionAttribute` — metadata attribute on endpoint classes, consumed by source generator
- `PermissionRegistryBuilder` — discovers permission constants via reflection (not changing in this phase)
- Test auth scheme in `SimpleModule.Tests.Shared` with `CreateAuthenticatedClient(params Claim[] claims)`

### Established Patterns
- Endpoints use `[RequirePermission("Module.Action")]` attribute for permission metadata
- Source generator emits `.RequireAuthorization()` on route groups for auto-registered endpoints
- Escape-hatch endpoints (via `ConfigureEndpoints` on module class) handle their own authorization
- Permission constants defined as `public const string` in module permission classes

### Integration Points
- `Program.cs` — authorization configuration (where fallback policy goes)
- `PermissionAuthorizationHandler` — where admin bypass is removed
- All endpoint classes — need `[AllowAnonymous]` audit
- `Users/Endpoints/Connect/` — login/register/logout endpoints (must stay public)
- `Dashboard/Views/HomeEndpoint.cs` — landing page (must stay public for unauthenticated users)
- Test infrastructure — needs permission claims in test helpers

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard ASP.NET Core authorization hardening patterns apply.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>
