---
outline: deep
---

# E2E Tests

End-to-end tests use [Playwright](https://playwright.dev/) to drive a real browser against a running SimpleModule application. They verify complete user flows including authentication, navigation, and CRUD operations.

## Setup

E2E tests live in `tests/e2e/` and are a separate npm workspace with their own `package.json`:

```json
{
  "name": "@simplemodule/e2e",
  "private": true,
  "scripts": {
    "test": "playwright test",
    "test:ui": "playwright test --ui",
    "test:headed": "playwright test --headed",
    "test:smoke": "playwright test tests/smoke/",
    "test:flows": "playwright test tests/flows/",
    "report": "playwright show-report"
  },
  "devDependencies": {
    "@faker-js/faker": "^10.4.0",
    "@playwright/test": "^1.58.2"
  }
}
```

Install Playwright browsers before first use:

```bash
npx playwright install
```

## Running Tests

From the repository root:

```bash
# Run all E2E tests
npm run test:e2e

# Run with Playwright UI (interactive mode)
npm run test:e2e:ui
```

Or from within the `tests/e2e/` directory:

```bash
npm test                          # all tests
npm run test:ui                   # interactive UI mode
npm run test:headed               # headed browser
npm run test:smoke                # smoke tests only
npm run test:flows                # flow tests only
npm run report                    # view last test report
```

## Configuration

Playwright is configured in `tests/e2e/playwright.config.ts`:

```typescript
const isCI = !!process.env.CI;
const baseURL = isCI ? 'http://localhost:5000' : 'https://localhost:5001';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: isCI,
  retries: 0,
  workers: isCI ? 1 : undefined,
  timeout: 15_000,
  expect: { timeout: 3_000 },
  reporter: [['html', {}], ...(isCI ? [['github', {}] as const] : [])],
  use: {
    baseURL,
    trace: 'on-first-retry',
    ignoreHTTPSErrors: true,
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
    },
  ],
  webServer: {
    command: isCI
      ? 'dotnet run --project ../../template/SimpleModule.Host --launch-profile http --no-build'
      : 'dotnet run --project ../../template/SimpleModule.Host',
    url: `${baseURL}/health/live`,
    reuseExistingServer: true,
    ignoreHTTPSErrors: true,
    timeout: 120_000,
    env: {
      ASPNETCORE_URLS: baseURL,
      Database__DefaultConnection: 'Data Source=e2e-test.db',
    },
  },
});
```

Key points:

- **`webServer`** -- Playwright automatically starts the .NET host if it is not already running, using the health endpoint to detect readiness. In CI the command switches to `--launch-profile http --no-build` against `http://localhost:5000`; locally it uses the default HTTPS profile on `https://localhost:5001`
- **`reuseExistingServer: true`** -- if you already have the app running (e.g., via `npm run dev`), Playwright uses it directly
- **Browser matrix** -- tests run on Chromium only (no Firefox or WebKit projects are configured)
- **Retries** -- retries are disabled (`0`) in both local and CI runs; failures surface immediately
- **Traces and screenshots** -- captured on first retry and on failure for debugging

## Authentication

E2E tests authenticate once in a setup project and reuse the auth state across all test files.

### Auth Setup

The `tests/auth/auth.setup.ts` file logs in as the admin user and saves the browser storage state:

```typescript
setup('authenticate as admin', async ({ page }) => {
  await page.goto('/Identity/Account/Login');
  await page.waitForURL('**/Identity/Account/Login**');

  await page.getByPlaceholder('you@example.com')
    .fill('admin@simplemodule.dev');
  await page.locator('input[type="password"]')
    .fill('Admin123!');
  await page.getByRole('button', { name: 'Log in' }).click();

  await page.waitForURL('/');
  await page.context().storageState({ path: authFile });
});
```

### Using Auth in Tests

Tests import a custom `test` fixture from `fixtures/base.ts` that automatically loads the saved auth state:

```typescript
import { test as base } from '@playwright/test';

const authFile = path.resolve(
  __dirname, '../auth/.auth/user.json');

export const test = base.extend({
  storageState: async (_, use) => {
    await use(authFile);
  },
});

export { expect } from '@playwright/test';
```

All test files import from this fixture instead of directly from `@playwright/test`:

```typescript
import { expect, test } from '../../fixtures/base';
```

## Test Organization

Tests are organized into two categories:

### Smoke Tests (`tests/smoke/`)

Quick page-load checks that verify pages render without errors:

```typescript
test.describe('Users pages', () => {
  test('users list loads', async ({ page }) => {
    const users = new UsersListPage(page);
    await users.goto();
    await expect(users.heading).toBeVisible();
  });

  test('create user page loads', async ({ page }) => {
    const create = new UsersCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });
});
```

### Flow Tests (`tests/flows/`)

Full CRUD and business workflows that create, read, update, and delete data:

```typescript
test.describe('Users CRUD', () => {
  test.describe.configure({ mode: 'serial' });

  test('create a user and verify it appears in the list',
    async ({ page }) => {
      const createPage = new UsersCreatePage(page);
      await createPage.goto();
      await createPage.createUser(userName, userEmail);

      const listPage = new UsersListPage(page);
      await listPage.goto();
      await expect(
        listPage.userRowByEmail(userEmail).first()
      ).toBeVisible();
    });
});
```

::: info Serial Mode
Flow tests use `test.describe.configure({ mode: 'serial' })` because they depend on data created by earlier tests in the same describe block (e.g., create then delete).
:::

## Page Object Model

E2E tests use the Page Object Model pattern. Each page has a corresponding class in `tests/e2e/pages/`:

```typescript
import type { Page } from '@playwright/test';

export class UsersListPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/users');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /users/i });
  }

  get userRows() {
    return this.page.locator('[data-testid="user-row"]');
  }

  userRowByEmail(email: string) {
    return this.page.getByText(email);
  }
}
```

Page objects are available for all module pages under `tests/e2e/pages/`:

```
pages/
  dashboard.page.ts
  admin/
    roles.page.ts
    roles-create.page.ts
    roles-edit.page.ts
    users.page.ts
    users-create.page.ts
    users-edit.page.ts
  audit-logs/
    browse.page.ts
    dashboard.page.ts
  background-jobs/
    dashboard.page.ts
    list.page.ts
    recurring.page.ts
  email/
  feature-flags/
  filestorage/
  openiddict/
    clients.page.ts
  rate-limiting/
  settings/
    admin.page.ts
    user.page.ts
    menu-manager.page.ts
  tenants/
    list.page.ts
  users/
    list.page.ts
    profile.page.ts
```

## CI Integration

In CI, the Playwright configuration automatically:

- Switches `baseURL` to `http://localhost:5000` and launches the host with `--launch-profile http --no-build`
- Limits to 1 worker for stability
- Adds the `github` reporter alongside HTML

```yaml
- name: Run E2E tests
  run: npm run test:e2e
  env:
    CI: true
```

Test results are available as an HTML report via `npm run report` (or `npx playwright show-report` from `tests/e2e/`).

## Next Steps

- [Deployment](/advanced/deployment) -- CI/CD pipeline and Docker configuration
- [CLI Overview](/cli/overview) -- project scaffolding and validation tools
- [Configuration Reference](/reference/configuration) -- all framework settings
