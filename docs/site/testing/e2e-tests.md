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
    "@playwright/test": "^1.52.0"
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

# Run only smoke tests
npm run test:e2e -- -- tests/smoke/

# Run only flow tests
npm run test:e2e -- -- tests/flows/
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
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html'], ...(process.env.CI ? [['github']] : [])],
  use: {
    baseURL: 'https://localhost:5001',
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
    // Firefox and WebKit are added in CI only
  ],
  webServer: {
    command: 'dotnet run --project ../../template/SimpleModule.Host',
    url: 'https://localhost:5001/health/live',
    reuseExistingServer: true,
    ignoreHTTPSErrors: true,
    timeout: 60_000,
    env: {
      ASPNETCORE_URLS: 'https://localhost:5001',
      Database__DefaultConnection: 'Data Source=e2e-test.db',
    },
  },
});
```

Key points:

- **`webServer`** -- Playwright automatically starts the .NET host if it is not already running, using the health endpoint to detect readiness
- **`reuseExistingServer: true`** -- if you already have the app running (e.g., via `npm run dev`), Playwright uses it directly
- **Browser matrix** -- locally tests run on Chromium only; CI adds Firefox and WebKit
- **Retries** -- CI retries failed tests twice; local runs have zero retries
- **Traces and screenshots** -- captured on first retry and on failure for debugging

## Authentication

E2E tests authenticate once in a setup project and reuse the auth state across all test files.

### Auth Setup

The `tests/auth/auth.setup.ts` file logs in as the admin user and saves the browser storage state:

```typescript
setup('authenticate as admin', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('link', { name: 'Log in' }).click();
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
test.describe('Products pages', () => {
  test('browse page loads', async ({ page }) => {
    const browse = new ProductsBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });

  test('create page loads', async ({ page }) => {
    const create = new ProductsCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });
});
```

### Flow Tests (`tests/flows/`)

Full CRUD and business workflows that create, read, update, and delete data:

```typescript
test.describe('Orders CRUD', () => {
  test.describe.configure({ mode: 'serial' });

  test('create an order and verify it appears in the list',
    async ({ page, request }) => {
      const productsPage = new ProductsCreatePage(page);
      await productsPage.goto();
      await productsPage.createProduct(productName, price);

      const createPage = new OrdersCreatePage(page);
      await createPage.goto();
      await createPage.createOrder(adminUserId, 0, quantity);

      const listPage = new OrdersListPage(page);
      await listPage.goto();
      await expect(
        listPage.orderRowByUser(adminUserId).first()
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

export class ProductsBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/browse');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /products/i });
  }

  get productCards() {
    return this.page.locator('[data-testid="product-card"]');
  }

  productByName(name: string) {
    return this.page.getByText(name);
  }
}
```

Page objects are available for all module pages under `tests/e2e/pages/`:

```
pages/
  dashboard.page.ts
  products/
    browse.page.ts
    create.page.ts
    edit.page.ts
    manage.page.ts
  orders/
    create.page.ts
    edit.page.ts
    list.page.ts
  settings/
    admin.page.ts
    user.page.ts
    menu-manager.page.ts
  pagebuilder/
    editor.page.ts
    manage.page.ts
    pages-list.page.ts
    viewer.page.ts
  openiddict/
    clients.page.ts
```

## CI Integration

In CI, the Playwright configuration automatically:

- Expands the browser matrix to include Firefox and WebKit
- Sets retries to 2
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
