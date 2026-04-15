# Error Page Templates (404/500/403)

## Overview

Add dedicated error page templates for 404, 500, and 403 errors with two layers: Inertia-rendered React pages for normal error scenarios, and a static HTML fallback for catastrophic failures.

## Current State

- `GlobalExceptionHandler` returns JSON `ProblemDetails` for all errors
- `app.tsx` shows toast notifications for HTTP errors during Inertia navigation
- `PageErrorBoundary` catches React rendering errors with a generic UI
- No dedicated error pages exist — errors are either toasts or generic boundary UI
- No `ForbiddenException` class exists (only `NotFoundException`, `ConflictException`, `ValidationException`)

## Design

### Layer 1: Inertia Error Pages (React)

Three components in `@simplemodule/ui`:

| Component | Status Code | When Shown |
|-----------|------------|------------|
| `ErrorPage404` | 404 | Route not found, `NotFoundException` |
| `ErrorPage500` | 500 | Unhandled server exceptions |
| `ErrorPage403` | 403 | Authorization failures, `ForbiddenException` |

**Visual design:** Minimal standalone (no sidebar/header). Centered content with:
- Large status code number (e.g., "404") in muted color
- Icon from lucide-react (SearchX for 404, AlertTriangle for 500, ShieldX for 403)
- Short heading ("Page not found", "Something went wrong", "Access denied")
- Brief description text
- "Go Home" button linking to `/`
- "Go Back" button using `window.history.back()`
- Uses existing Tailwind theme tokens (works with dark mode)

**Location:** `packages/SimpleModule.UI/components/errors/`
- `error-page-404.tsx`
- `error-page-500.tsx`
- `error-page-403.tsx`
- `error-page-layout.tsx` — shared layout wrapper (centering, max-width, theme tokens)

### Layer 2: Static HTML Fallback

A single `error.html` in `template/SimpleModule.Host/wwwroot/` with:
- Inline CSS only (no external dependencies)
- Respects dark mode via `prefers-color-scheme` media query
- Displays: status code, title, description, "Go Home" link
- Status code/message injected via simple string replacement in middleware
- Works when React/JS/Inertia all fail

### Backend Changes

#### 1. New `ForbiddenException` class

`framework/SimpleModule.Core/Exceptions/ForbiddenException.cs`
- Sealed class extending `Exception`
- Same pattern as `NotFoundException`

#### 2. Update `GlobalExceptionHandler`

Add `ForbiddenException` → 403 mapping alongside existing exception mappings.

For requests that accept HTML (non-API, non-XHR): render an Inertia error page instead of JSON. Decision logic:
- If request has `X-Inertia` header or `Accept: text/html` → render Inertia error page
- Otherwise → return JSON `ProblemDetails` (current behavior, unchanged)

The Inertia error page rendering calls `Inertia.Render("Error/{StatusCode}", props)` where props include `status`, `title`, and `message`.

#### 3. Error page endpoint

Add a catch-all error endpoint in the framework (`SimpleModule.Hosting`) that:
- Registers error page routes via `UseStatusCodePagesWithReExecute("/error/{0}")` 
- Has an error controller/endpoint that renders the appropriate Inertia error page
- Handles bare 404s from routing (no matching route) and 403s from authorization middleware

#### 4. Update middleware pipeline

In `SimpleModuleHostExtensions.UseSimpleModuleInfrastructure()`:
- Add `UseStatusCodePagesWithReExecute("/error/{0}")` before `UseExceptionHandler()`
- Configure `UseExceptionHandler` fallback to serve `error.html` when Inertia rendering fails

### Frontend Changes

#### 1. Error page components in `@simplemodule/ui`

New exports from the UI package. Each component receives props: `{ status: number, title: string, message: string }`.

#### 2. Host ClientApp registers error pages

In `app.tsx`, the page resolver handles `Error/*` pages by importing from `@simplemodule/ui`:
- `Error/404` → `ErrorPage404`
- `Error/500` → `ErrorPage500`  
- `Error/403` → `ErrorPage403`

#### 3. Update `httpException` handler

In `app.tsx`, the `router.on('httpException')` handler changes from toast to Inertia visit:
- For 404/403/500: navigate to the error page via `router.visit` with error props
- For other errors: keep toast behavior

## File Changes Summary

| File | Action |
|------|--------|
| `framework/SimpleModule.Core/Exceptions/ForbiddenException.cs` | Create |
| `framework/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs` | Modify — add 403 mapping, Inertia error rendering |
| `framework/SimpleModule.Core/Constants/ErrorMessages.cs` | Modify — add forbidden message constants |
| `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs` | Modify — add status code pages middleware, error endpoint |
| `packages/SimpleModule.UI/components/errors/error-page-layout.tsx` | Create |
| `packages/SimpleModule.UI/components/errors/error-page-404.tsx` | Create |
| `packages/SimpleModule.UI/components/errors/error-page-500.tsx` | Create |
| `packages/SimpleModule.UI/components/errors/error-page-403.tsx` | Create |
| `packages/SimpleModule.UI/index.ts` | Modify — export error components |
| `template/SimpleModule.Host/wwwroot/error.html` | Create |
| `template/SimpleModule.Host/ClientApp/app.tsx` | Modify — register error pages, update httpException handler |

## Out of Scope

- Error logging/monitoring integration (Sentry, etc.)
- Custom error pages per module
- Retry logic for transient failures
- Error analytics/tracking
