# Error Page Templates (404/500/403) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add dedicated error page templates (404, 500, 403) with Inertia-rendered React pages and a static HTML fallback for catastrophic failures.

**Architecture:** Two-layer approach. Layer 1: React error page components in `@simplemodule/ui` rendered via Inertia when the backend catches exceptions or the routing pipeline produces bare status codes. Layer 2: A static `error.html` served when React/Inertia itself fails. The `GlobalExceptionHandler` is extended to render Inertia error pages for browser requests and keeps JSON ProblemDetails for API requests. `UseStatusCodePagesWithReExecute` catches bare 404s/403s from the routing pipeline.

**Tech Stack:** ASP.NET (.NET 10), React 19, Inertia.js, Tailwind CSS, lucide-react, xUnit + FluentAssertions

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `framework/SimpleModule.Core/Exceptions/ForbiddenException.cs` | Create | 403 exception class |
| `framework/SimpleModule.Core/Constants/ErrorMessages.cs` | Modify | Add forbidden message constants |
| `framework/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs` | Modify | Add 403 mapping + Inertia error rendering for browser requests |
| `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs` | Modify | Add `UseStatusCodePagesWithReExecute`, error endpoint |
| `tests/SimpleModule.Core.Tests/GlobalExceptionHandlerTests.cs` | Modify | Add 403 test + Inertia rendering tests |
| `packages/SimpleModule.UI/components/errors/error-page-layout.tsx` | Create | Shared centering/styling wrapper for all error pages |
| `packages/SimpleModule.UI/components/errors/error-page-404.tsx` | Create | 404 error page component |
| `packages/SimpleModule.UI/components/errors/error-page-500.tsx` | Create | 500 error page component |
| `packages/SimpleModule.UI/components/errors/error-page-403.tsx` | Create | 403 error page component |
| `packages/SimpleModule.UI/components/errors/index.ts` | Create | Barrel export for error components |
| `packages/SimpleModule.UI/package.json` | Modify | Add `./errors` export entry |
| `packages/SimpleModule.UI/components/layouts/layout-provider.tsx` | Modify | Skip auto-layout for error pages |
| `template/SimpleModule.Host/ClientApp/app.tsx` | Modify | Resolve `Error/*` pages, update `httpException` handler |
| `template/SimpleModule.Host/wwwroot/error.html` | Create | Static HTML fallback for catastrophic failures |

---

## Task 1: ForbiddenException and Error Constants

**Files:**
- Create: `framework/SimpleModule.Core/Exceptions/ForbiddenException.cs`
- Modify: `framework/SimpleModule.Core/Constants/ErrorMessages.cs`
- Test: `tests/SimpleModule.Core.Tests/GlobalExceptionHandlerTests.cs`

- [ ] **Step 1: Create ForbiddenException class**

Create `framework/SimpleModule.Core/Exceptions/ForbiddenException.cs`:

```csharp
using SimpleModule.Core.Constants;

namespace SimpleModule.Core.Exceptions;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException()
        : base(ErrorMessages.DefaultForbiddenMessage) { }

    public ForbiddenException(string message)
        : base(message) { }

    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

- [ ] **Step 2: Add forbidden constants to ErrorMessages**

In `framework/SimpleModule.Core/Constants/ErrorMessages.cs`, add after the `ConflictTitle` line:

```csharp
public const string ForbiddenTitle = "Forbidden";
```

And after the `DefaultConflictMessage` line:

```csharp
public const string DefaultForbiddenMessage = "You do not have permission to access this resource.";
```

- [ ] **Step 3: Add ForbiddenException mapping to GlobalExceptionHandler**

In `framework/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs`, add a new case in the switch expression (line 31, after the `ConflictException` case):

```csharp
ForbiddenException => (StatusCodes.Status403Forbidden, ErrorMessages.ForbiddenTitle, null),
```

- [ ] **Step 4: Write failing test for ForbiddenException handling**

Add to `tests/SimpleModule.Core.Tests/GlobalExceptionHandlerTests.cs`:

```csharp
[Fact]
public async Task ForbiddenException_Returns403()
{
    var context = CreateHttpContext();
    var exception = new ForbiddenException("Access denied to admin panel");

    var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

    handled.Should().BeTrue();
    context.Response.StatusCode.Should().Be(403);

    var doc = await ReadResponseBodyAsync(context);
    doc.RootElement.GetProperty("title").GetString().Should().Be("Forbidden");
    doc.RootElement.GetProperty("detail").GetString().Should().Be("Access denied to admin panel");
}
```

- [ ] **Step 5: Run tests to verify**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "GlobalExceptionHandler" -v minimal`
Expected: All tests pass including the new `ForbiddenException_Returns403`.

- [ ] **Step 6: Commit**

```bash
git add framework/SimpleModule.Core/Exceptions/ForbiddenException.cs framework/SimpleModule.Core/Constants/ErrorMessages.cs framework/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs tests/SimpleModule.Core.Tests/GlobalExceptionHandlerTests.cs
git commit -m "feat: add ForbiddenException with 403 mapping in GlobalExceptionHandler"
```

---

## Task 2: Inertia Error Rendering in GlobalExceptionHandler

**Files:**
- Modify: `framework/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs`
- Test: `tests/SimpleModule.Core.Tests/GlobalExceptionHandlerTests.cs`

- [ ] **Step 1: Write failing test for Inertia error rendering**

Add to `tests/SimpleModule.Core.Tests/GlobalExceptionHandlerTests.cs`:

```csharp
[Fact]
public async Task InertiaRequest_NotFoundException_ReturnsInertiaErrorPage()
{
    var context = CreateHttpContext();
    context.Request.Headers["X-Inertia"] = "true";
    context.Request.Headers["X-Inertia-Version"] = "1";
    var exception = new NotFoundException("Product", 42);

    var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

    handled.Should().BeTrue();
    context.Response.StatusCode.Should().Be(404);
    context.Response.Headers["X-Inertia"].ToString().Should().Be("true");
    context.Response.ContentType.Should().Contain("application/json");

    var doc = await ReadResponseBodyAsync(context);
    doc.RootElement.GetProperty("component").GetString().Should().Be("Error/404");
    doc.RootElement.GetProperty("props").GetProperty("status").GetInt32().Should().Be(404);
    doc.RootElement.GetProperty("props").GetProperty("title").GetString().Should().Be("Not Found");
}

[Fact]
public async Task NonInertiaRequest_NotFoundException_ReturnsJsonProblemDetails()
{
    var context = CreateHttpContext();
    // No X-Inertia header = API request
    var exception = new NotFoundException("Product", 42);

    var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

    handled.Should().BeTrue();
    context.Response.StatusCode.Should().Be(404);

    var doc = await ReadResponseBodyAsync(context);
    doc.RootElement.GetProperty("title").GetString().Should().Be("Not Found");
    // Should NOT have Inertia fields
    doc.RootElement.TryGetProperty("component", out _).Should().BeFalse();
}

[Fact]
public async Task InertiaRequest_UnhandledException_ReturnsInertia500_WithoutSensitiveDetails()
{
    var context = CreateHttpContext();
    context.Request.Headers["X-Inertia"] = "true";
    context.Request.Headers["X-Inertia-Version"] = "1";
    var exception = new InvalidOperationException("Sensitive DB error");

    var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

    handled.Should().BeTrue();
    context.Response.StatusCode.Should().Be(500);

    var doc = await ReadResponseBodyAsync(context);
    doc.RootElement.GetProperty("component").GetString().Should().Be("Error/500");
    var props = doc.RootElement.GetProperty("props");
    props.GetProperty("message").GetString().Should().NotContain("Sensitive DB error");
    props.GetProperty("message").GetString().Should().Be("An unexpected error occurred. Please try again later.");
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "InertiaRequest" -v minimal`
Expected: FAIL — the handler currently always writes ProblemDetails JSON, not Inertia responses.

- [ ] **Step 3: Implement Inertia error rendering in GlobalExceptionHandler**

Replace the full content of `framework/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs`:

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Constants;
using SimpleModule.Core.Inertia;

namespace SimpleModule.Core.Exceptions;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private static readonly JsonSerializerOptions InertiaJsonOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                ErrorMessages.ValidationErrorTitle,
                ve.Errors
            ),
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                ErrorMessages.ValidationErrorTitle,
                null
            ),
            NotFoundException => (StatusCodes.Status404NotFound, ErrorMessages.NotFoundTitle, null),
            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                ErrorMessages.ForbiddenTitle,
                null
            ),
            ConflictException => (StatusCodes.Status409Conflict, ErrorMessages.ConflictTitle, null),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorMessages.InternalServerErrorTitle,
                null
            ),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            logger.LogWarning(
                exception,
                "Handled exception occurred: {Message}",
                exception.Message
            );
        }

        var detail =
            statusCode == StatusCodes.Status500InternalServerError
                ? ErrorMessages.UnexpectedError
                : exception.Message;

        httpContext.Response.StatusCode = statusCode;

        // Inertia requests get an Inertia error page response
        if (httpContext.Request.Headers.ContainsKey("X-Inertia"))
        {
            return await WriteInertiaErrorAsync(httpContext, statusCode, title, detail);
        }

        // API/non-Inertia requests get ProblemDetails JSON
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
        };

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static async ValueTask<bool> WriteInertiaErrorAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string message
    )
    {
        var component = $"Error/{statusCode}";
        var props = new { status = statusCode, title, message };

        var pageData = new
        {
            component,
            props,
            url = httpContext.Request.Path + httpContext.Request.QueryString,
            version = InertiaMiddleware.Version,
        };

        httpContext.Response.Headers["X-Inertia"] = "true";
        httpContext.Response.Headers["Vary"] = "X-Inertia";
        httpContext.Response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(pageData, InertiaJsonOptions);
        await httpContext.Response.WriteAsync(json);
        return true;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "GlobalExceptionHandler" -v minimal`
Expected: All tests pass (existing + new Inertia tests).

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Core/Exceptions/GlobalExceptionHandler.cs tests/SimpleModule.Core.Tests/GlobalExceptionHandlerTests.cs
git commit -m "feat: render Inertia error pages for browser requests in GlobalExceptionHandler"
```

---

## Task 3: Status Code Pages Middleware and Error Endpoint

**Files:**
- Modify: `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs`

- [ ] **Step 1: Add error endpoint and status code pages middleware**

In `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs`:

**Add import** at the top of the file (after existing usings):

```csharp
using SimpleModule.Core.Inertia;
```

**Add `UseStatusCodePagesWithReExecute`** in the `UseSimpleModuleInfrastructure` method, right after line 151 (`app.UseExceptionHandler();`):

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

**Add error endpoint mapping** at the end of the `UseSimpleModuleInfrastructure` method, right before the closing brace (after the health checks block, around line 242):

```csharp
app.MapGet(
                "/error/{statusCode:int}",
                (int statusCode) =>
                {
                    var (title, message) = statusCode switch
                    {
                        403 => (
                            ErrorMessages.ForbiddenTitle,
                            ErrorMessages.DefaultForbiddenMessage
                        ),
                        404 => (
                            ErrorMessages.NotFoundTitle,
                            ErrorMessages.DefaultNotFoundMessage
                        ),
                        _ => (
                            ErrorMessages.InternalServerErrorTitle,
                            ErrorMessages.UnexpectedError
                        ),
                    };

                    return Inertia.Render(
                        $"Error/{statusCode}",
                        new
                        {
                            status = statusCode,
                            title,
                            message,
                        }
                    );
                }
            )
            .AllowAnonymous()
            .ExcludeFromDescription();
```

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build framework/SimpleModule.Hosting/SimpleModule.Hosting.csproj`
Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs
git commit -m "feat: add status code pages middleware and /error/{statusCode} endpoint"
```

---

## Task 4: React Error Page Components

**Files:**
- Create: `packages/SimpleModule.UI/components/errors/error-page-layout.tsx`
- Create: `packages/SimpleModule.UI/components/errors/error-page-404.tsx`
- Create: `packages/SimpleModule.UI/components/errors/error-page-500.tsx`
- Create: `packages/SimpleModule.UI/components/errors/error-page-403.tsx`
- Create: `packages/SimpleModule.UI/components/errors/index.ts`
- Modify: `packages/SimpleModule.UI/package.json`

- [ ] **Step 1: Create shared error page layout**

Create `packages/SimpleModule.UI/components/errors/error-page-layout.tsx`:

```tsx
import type * as React from 'react';
import { DarkModeToggle } from '../layouts/dark-mode-toggle';

interface ErrorPageLayoutProps {
  statusCode: number;
  icon: React.ReactNode;
  title: string;
  description: string;
}

export function ErrorPageLayout({ statusCode, icon, title, description }: ErrorPageLayoutProps) {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-surface px-4">
      <div className="absolute top-4 right-4">
        <DarkModeToggle />
      </div>
      <div className="w-full max-w-md text-center">
        <p className="text-[8rem] leading-none font-bold text-text-muted/20">{statusCode}</p>
        <div className="mx-auto -mt-4 mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-danger-bg">
          {icon}
        </div>
        <h1 className="text-2xl font-semibold text-text">{title}</h1>
        <p className="mt-2 text-sm text-text-muted">{description}</p>
        <div className="mt-8 flex justify-center gap-3">
          <a
            href="/"
            className="inline-flex items-center justify-center rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90"
          >
            Go home
          </a>
          <button
            type="button"
            onClick={() => window.history.back()}
            className="inline-flex items-center justify-center rounded-lg border border-border bg-surface px-4 py-2 text-sm font-medium text-text hover:bg-surface-raised"
          >
            Go back
          </button>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: Create 404 error page**

Create `packages/SimpleModule.UI/components/errors/error-page-404.tsx`:

```tsx
import { ErrorPageLayout } from './error-page-layout';

interface Props {
  status?: number;
  title?: string;
  message?: string;
}

export default function ErrorPage404({ message }: Props) {
  return (
    <ErrorPageLayout
      statusCode={404}
      icon={
        <svg
          className="h-7 w-7 text-danger-text"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607ZM13.5 10.5h-6"
          />
        </svg>
      }
      title="Page not found"
      description={message ?? 'The page you are looking for does not exist or has been moved.'}
    />
  );
}
```

- [ ] **Step 3: Create 500 error page**

Create `packages/SimpleModule.UI/components/errors/error-page-500.tsx`:

```tsx
import { ErrorPageLayout } from './error-page-layout';

interface Props {
  status?: number;
  title?: string;
  message?: string;
}

export default function ErrorPage500({ message }: Props) {
  return (
    <ErrorPageLayout
      statusCode={500}
      icon={
        <svg
          className="h-7 w-7 text-danger-text"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
          />
        </svg>
      }
      title="Something went wrong"
      description={message ?? 'An unexpected error occurred. Please try again later.'}
    />
  );
}
```

- [ ] **Step 4: Create 403 error page**

Create `packages/SimpleModule.UI/components/errors/error-page-403.tsx`:

```tsx
import { ErrorPageLayout } from './error-page-layout';

interface Props {
  status?: number;
  title?: string;
  message?: string;
}

export default function ErrorPage403({ message }: Props) {
  return (
    <ErrorPageLayout
      statusCode={403}
      icon={
        <svg
          className="h-7 w-7 text-danger-text"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.5"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z"
          />
        </svg>
      }
      title="Access denied"
      description={message ?? 'You do not have permission to access this resource.'}
    />
  );
}
```

- [ ] **Step 5: Create barrel export**

Create `packages/SimpleModule.UI/components/errors/index.ts`:

```typescript
export { default as ErrorPage403 } from './error-page-403';
export { default as ErrorPage404 } from './error-page-404';
export { default as ErrorPage500 } from './error-page-500';
export { ErrorPageLayout } from './error-page-layout';
```

- [ ] **Step 6: Add `./errors` export to package.json**

In `packages/SimpleModule.UI/package.json`, add to the `"exports"` object:

```json
"./errors": "./components/errors/index.ts"
```

- [ ] **Step 7: Verify frontend builds**

Run: `npm run check` (from repo root)
Expected: No lint or format errors.

- [ ] **Step 8: Commit**

```bash
git add packages/SimpleModule.UI/components/errors/ packages/SimpleModule.UI/package.json
git commit -m "feat: add React error page components (404, 500, 403) in @simplemodule/ui"
```

---

## Task 5: Wire Error Pages into ClientApp

**Files:**
- Modify: `template/SimpleModule.Host/ClientApp/app.tsx`
- Modify: `packages/SimpleModule.UI/components/layouts/layout-provider.tsx`

- [ ] **Step 1: Update app.tsx to resolve error pages and redirect on httpException**

In `template/SimpleModule.Host/ClientApp/app.tsx`:

**Add imports** after the existing imports (line 4):

```typescript
import { ErrorPage404 } from '@simplemodule/ui/errors';
import { ErrorPage403 } from '@simplemodule/ui/errors';
import { ErrorPage500 } from '@simplemodule/ui/errors';
```

**Replace the `httpException` handler** (lines 100-119) with:

```typescript
// Handle non-Inertia error responses (404, 500, etc.) by showing a toast
// for minor errors, or letting Inertia render error pages for page-level errors.
router.on('httpException', (event) => {
  event.preventDefault();

  const response = event.detail.response;
  const body = response.data as { detail?: string; title?: string } | string | undefined;
  let parsed: { detail?: string; title?: string } | undefined;
  if (typeof body === 'string') {
    try {
      parsed = JSON.parse(body);
    } catch {
      // non-JSON response body
    }
  } else {
    parsed = body;
  }
  const message = parsed?.detail ?? parsed?.title ?? `Server error (${response.status})`;
  showErrorToast(message);
});
```

**Replace the `resolve` function** inside `createInertiaApp` (lines 145-153) with:

```typescript
  resolve: async (name) => {
    // Error pages are bundled in ClientApp, not in module pages.js files
    const errorPages: Record<string, { default: React.ComponentType<any> }> = {
      'Error/404': { default: ErrorPage404 },
      'Error/403': { default: ErrorPage403 },
      'Error/500': { default: ErrorPage500 },
    };

    if (name in errorPages) {
      return errorPages[name];
    }

    try {
      const page = await resolvePage(name);
      return resolveLayout(page);
    } catch (err) {
      showErrorToast(`Failed to load page "${name}". Try refreshing the page.`);
      throw err;
    }
  },
```

Note: Error pages intentionally skip `resolveLayout` — they have no layout wrapper (minimal standalone design).

- [ ] **Step 2: Add React import for type annotation**

At the top of `app.tsx`, add if not present:

```typescript
import type React from 'react';
```

- [ ] **Step 3: Verify frontend builds**

Run: `npm run check` (from repo root)
Expected: No lint or format errors.

- [ ] **Step 4: Commit**

```bash
git add template/SimpleModule.Host/ClientApp/app.tsx
git commit -m "feat: wire error page resolution and httpException handling in ClientApp"
```

---

## Task 6: Static HTML Fallback Page

**Files:**
- Create: `template/SimpleModule.Host/wwwroot/error.html`

- [ ] **Step 1: Create static error.html**

Create `template/SimpleModule.Host/wwwroot/error.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="color-scheme" content="light dark" />
    <title>Error - SimpleModule</title>
    <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: 'DM Sans', system-ui, -apple-system, sans-serif;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            background: #fafafa;
            color: #1a1a1a;
            padding: 1rem;
        }
        @media (prefers-color-scheme: dark) {
            body { background: #0a0a0a; color: #e5e5e5; }
            .card { background: #171717; border-color: #262626; }
            .code { color: rgba(229, 229, 229, 0.15); }
            .btn-primary { background: #059669; }
            .btn-secondary { background: #171717; border-color: #262626; color: #e5e5e5; }
            .btn-secondary:hover { background: #262626; }
        }
        .container { text-align: center; max-width: 28rem; width: 100%; }
        .code {
            font-size: 8rem;
            font-weight: 700;
            line-height: 1;
            color: rgba(0, 0, 0, 0.08);
        }
        .icon {
            width: 3.5rem; height: 3.5rem;
            margin: -1rem auto 1rem;
            background: #fef2f2;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        @media (prefers-color-scheme: dark) {
            .icon { background: #450a0a; }
            .icon svg { color: #fca5a5; }
        }
        .icon svg { width: 1.75rem; height: 1.75rem; color: #dc2626; }
        h1 { font-size: 1.5rem; font-weight: 600; margin-bottom: 0.5rem; }
        p { font-size: 0.875rem; color: #737373; line-height: 1.5; }
        .actions { margin-top: 2rem; display: flex; gap: 0.75rem; justify-content: center; }
        .btn {
            display: inline-flex; align-items: center; justify-content: center;
            padding: 0.5rem 1rem; border-radius: 0.5rem;
            font-size: 0.875rem; font-weight: 500;
            text-decoration: none; border: 1px solid transparent;
            cursor: pointer;
        }
        .btn-primary { background: #059669; color: #fff; }
        .btn-primary:hover { opacity: 0.9; }
        .btn-secondary { background: #fff; border-color: #e5e5e5; color: #1a1a1a; }
        .btn-secondary:hover { background: #f5f5f5; }
    </style>
</head>
<body>
    <div class="container">
        <p class="code">500</p>
        <div class="icon">
            <svg fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
            </svg>
        </div>
        <h1>Something went wrong</h1>
        <p>An unexpected error occurred. Please try again later.</p>
        <div class="actions">
            <a href="/" class="btn btn-primary">Go home</a>
            <button type="button" onclick="history.back()" class="btn btn-secondary">Go back</button>
        </div>
    </div>
</body>
</html>
```

- [ ] **Step 2: Configure exception handler fallback to error.html**

In `framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs`, replace the plain `app.UseExceptionHandler();` call (line 151) with:

```csharp
app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                // If GlobalExceptionHandler already wrote the response, do nothing
                if (context.Response.HasStarted)
                    return;

                // Fallback: serve static error.html for catastrophic failures
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "text/html";
                var errorPage = Path.Combine(
                    app.Environment.WebRootPath,
                    "error.html"
                );
                if (File.Exists(errorPage))
                {
                    await context.Response.SendFileAsync(errorPage);
                }
                else
                {
                    await context.Response.WriteAsync(
                        "<h1>500 Internal Server Error</h1>"
                    );
                }
            });
        });
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build framework/SimpleModule.Hosting/SimpleModule.Hosting.csproj`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add template/SimpleModule.Host/wwwroot/error.html framework/SimpleModule.Hosting/SimpleModuleHostExtensions.cs
git commit -m "feat: add static error.html fallback and configure exception handler"
```

---

## Task 7: Build and Verify End-to-End

- [ ] **Step 1: Run full backend build**

Run: `dotnet build`
Expected: Build succeeds with no errors or warnings.

- [ ] **Step 2: Run all backend tests**

Run: `dotnet test --filter "GlobalExceptionHandler" -v minimal`
Expected: All tests pass.

- [ ] **Step 3: Run frontend checks**

Run: `npm run check`
Expected: No lint or format errors.

- [ ] **Step 4: Run frontend build**

Run: `npm run build`
Expected: Build succeeds. Error page components are bundled into ClientApp's `app.js`.

- [ ] **Step 5: Commit any final fixes if needed**

If any build/lint fixes were needed, commit them:

```bash
git add -A
git commit -m "fix: resolve build/lint issues in error pages implementation"
```
