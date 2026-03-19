# OpenIddict Table Ownership + Client Management UI

**Date**: 2026-03-19
**Branch**: feature/permission-system
**Goal**: Fix OpenIddict table prefixes and add a client management UI to the OpenIddict module.

## 1. Fix OpenIddict Table Prefixes

The source generator's HostDbContext emitter currently assigns unprefixed entities to `Users_` schema. OpenIddict entities need their own `OpenIddict_` prefix (SQLite) or `openiddict` schema (PostgreSQL/SQL Server).

- Update the source generator to detect OpenIddict entity types and prefix them with `OpenIddict_`
- Reset the migration after the fix

## 2. Client Management UI

**Module:** OpenIddict (at `/openiddict/clients`)

### View Endpoints (IViewEndpoint)
- `GET /openiddict/clients` — list all registered clients
- `GET /openiddict/clients/create` — create form
- `GET /openiddict/clients/{id}/edit` — edit form with tabbed sections

### Action Endpoints (IEndpoint)
- `POST /openiddict/clients` — create client
- `POST /openiddict/clients/{id}` — update client details
- `DELETE /openiddict/clients/{id}` — delete client

### React Pages
- `ClientsPage` — table listing all clients (ID, display name, client type, grant types)
- `ClientsCreatePage` — form: client ID, display name, client type (public/confidential), secret (for confidential), redirect URIs, post-logout URIs, grant types (checkboxes), scopes (checkboxes)
- `ClientsEditPage` — tabbed edit: Details tab, URIs tab, Permissions tab

### Authorization
Require Admin role (same as existing admin pages).

### Data Access
Use `IOpenIddictApplicationManager` from OpenIddict's built-in DI — no direct DB queries.

### Permissions
Add `OpenIddictPermissions.ManageClients` to the OpenIddict module's `ConfigurePermissions`.

## Dependency Graph (unchanged)

```
OpenIddict ──→ Users.csproj + Permissions.Contracts
```
