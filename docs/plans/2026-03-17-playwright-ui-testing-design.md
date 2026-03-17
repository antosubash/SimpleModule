# Playwright UI Testing Design

## Summary

Add browser-level UI testing using Playwright (Node.js) to the SimpleModule project. Tests live in a single `tests/e2e/` project covering all modules, with smoke tests for every page and full CRUD flow tests for critical paths.

## Decisions

| Decision | Choice |
|----------|--------|
| Test scope | Smoke tests (all pages) + CRUD flow tests (critical paths) |
| Test location | `tests/e2e/` (single project, top-level) |
| Server management | `webServer` config with `reuseExistingServer: true` |
| Authentication | Seed test user on startup, login via UI, store auth state |
| Browsers | Chromium locally, Chromium + Firefox + WebKit in CI |
| Data strategy | Fresh DB per run, each test seeds its own data via UI |
| Framework | Pure Node.js Playwright (TypeScript) |

## Project Structure

```
tests/e2e/
├── playwright.config.ts
├── package.json
├── tsconfig.json
├── .gitignore
├── fixtures/
│   └── base.ts                 # Extended test fixture with auth
├── pages/                      # Page Object Models
│   ├── login.page.ts
│   ├── dashboard.page.ts
│   ├── products/
│   │   ├── browse.page.ts
│   │   ├── create.page.ts
│   │   ├── edit.page.ts
│   │   └── manage.page.ts
│   └── orders/
│       ├── list.page.ts
│       ├── create.page.ts
│       └── edit.page.ts
├── tests/
│   ├── auth/
│   │   └── auth.setup.ts       # Login + store auth state
│   ├── smoke/
│   │   ├── dashboard.spec.ts
│   │   ├── products.spec.ts
│   │   └── orders.spec.ts
│   └── flows/
│       ├── products-crud.spec.ts
│       └── orders-crud.spec.ts
└── auth/
    └── .auth/                  # Stored auth state (gitignored)
```

## Playwright Configuration

- `baseURL`: `https://localhost:5001`
- `ignoreHTTPSErrors: true` for self-signed dev cert
- `webServer` runs `dotnet run --project ../../template/SimpleModule.Host`, waits on `/health/live`
- `reuseExistingServer: true` — skips startup if server already running
- Auth setup project runs first, stores browser state for all test projects
- Cross-browser (Firefox, WebKit) enabled only when `process.env.CI` is set
- Retries: 2 in CI, 0 locally
- Traces captured on first retry, screenshots on failure

## Authentication

1. Host seeds a test user (`testuser@example.com` / `TestPassword123!`) on startup in Development environment
2. `auth.setup.ts` logs in via the UI and stores `storageState` to `auth/.auth/user.json`
3. `fixtures/base.ts` extends Playwright's `test` to use stored auth state automatically
4. All test files import from `fixtures/base.ts` instead of `@playwright/test`

## Test Patterns

**Smoke tests**: Verify each page loads and renders key elements (headings, tables, forms). Fast, independent, one assertion per test.

**CRUD flow tests**: Full user journeys — create an entity via the UI, verify it appears, edit it, verify changes, delete it, verify removal. Each test is self-contained with no shared state.

**Selectors**: Prefer accessible selectors (`getByRole`, `getByLabel`, `getByText`) over CSS selectors.

## Integration

- `tests/e2e` added as an npm workspace
- Root scripts: `test:e2e`, `test:e2e:ui`
- CI: `npx playwright install --with-deps` before test run
- HTML report uploaded as CI artifact on failure

## Backend Changes

- Add `SeedTestUser()` to Host startup, gated behind `app.Environment.IsDevelopment()`
- Creates a known test user in the Users DB on each fresh database
