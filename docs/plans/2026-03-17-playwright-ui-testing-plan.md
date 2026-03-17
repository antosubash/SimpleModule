# Playwright UI Testing Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add browser-level UI testing with Playwright covering smoke tests for all pages and CRUD flow tests for critical paths.

**Architecture:** Single `tests/e2e/` Node.js project using Playwright Test runner with TypeScript. Tests run against `https://localhost:5001` with the server auto-started via Playwright's `webServer` config. Auth uses the pre-seeded admin user (`admin@simplemodule.dev` / `Admin123!`) logging in via the OAuth2 UI flow, with browser state stored for reuse.

**Tech Stack:** Playwright Test, TypeScript, npm workspace

---

### Task 1: Scaffold the E2E project

**Files:**
- Create: `tests/e2e/package.json`
- Create: `tests/e2e/tsconfig.json`
- Create: `tests/e2e/.gitignore`
- Modify: `package.json` (root — add workspace + scripts)
- Modify: `biome.json` (add `tests/**` to includes)

**Step 1: Create `tests/e2e/package.json`**

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
    "@playwright/test": "^1.52.0"
  }
}
```

**Step 2: Create `tests/e2e/tsconfig.json`**

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "strict": true,
    "noEmit": true,
    "skipLibCheck": true
  },
  "include": ["**/*.ts"]
}
```

**Step 3: Create `tests/e2e/.gitignore`**

```
test-results/
playwright-report/
blob-report/
auth/.auth/
```

**Step 4: Add workspace and scripts to root `package.json`**

Add `"tests/e2e"` to the `workspaces` array. Add scripts:

```json
{
  "scripts": {
    "test:e2e": "npm run test -w tests/e2e",
    "test:e2e:ui": "npm run test:ui -w tests/e2e"
  }
}
```

**Step 5: Add `tests/**` to biome.json includes**

In `biome.json`, update the `files.includes` array to:

```json
["modules/**", "packages/**", "template/**", "tests/**", "!**/wwwroot/**"]
```

**Step 6: Install dependencies**

Run: `npm install`

**Step 7: Install Playwright browsers**

Run: `npx -w tests/e2e playwright install --with-deps chromium`

**Step 8: Commit**

```bash
git add tests/e2e/package.json tests/e2e/tsconfig.json tests/e2e/.gitignore package.json biome.json package-lock.json
git commit -m "chore: scaffold Playwright E2E test project"
```

---

### Task 2: Playwright configuration

**Files:**
- Create: `tests/e2e/playwright.config.ts`

**Step 1: Create `tests/e2e/playwright.config.ts`**

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html'], ...(process.env.CI ? [['github' as const]] : [])],
  use: {
    baseURL: 'https://localhost:5001',
    trace: 'on-first-retry',
    ignoreHTTPSErrors: true,
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
    },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
    },
    ...(process.env.CI
      ? [
          {
            name: 'firefox',
            use: { ...devices['Desktop Firefox'] },
            dependencies: ['setup'],
          },
          {
            name: 'webkit',
            use: { ...devices['Desktop Safari'] },
            dependencies: ['setup'],
          },
        ]
      : []),
  ],
  webServer: {
    command: 'dotnet run --project ../../template/SimpleModule.Host',
    url: 'https://localhost:5001/health/live',
    reuseExistingServer: true,
    ignoreHTTPSErrors: true,
    timeout: 60_000,
  },
});
```

**Step 2: Commit**

```bash
git add tests/e2e/playwright.config.ts
git commit -m "chore: add Playwright configuration"
```

---

### Task 3: Auth setup and base fixture

**Files:**
- Create: `tests/e2e/tests/auth/auth.setup.ts`
- Create: `tests/e2e/fixtures/base.ts`

**Step 1: Create `tests/e2e/tests/auth/auth.setup.ts`**

The app uses OAuth2/OpenIddict with PKCE. The Dashboard Home page (`/`) shows a login button for unauthenticated users. Clicking it initiates the OAuth2 flow which presents a login form. After login, the user is redirected back to the app.

```typescript
import { test as setup, expect } from '@playwright/test';

const authFile = 'auth/.auth/user.json';

setup('authenticate as admin', async ({ page }) => {
  // Navigate to the app — unauthenticated users see the landing page with login button
  await page.goto('/');

  // Click the login button to start OAuth2 flow
  await page.getByRole('link', { name: /log\s*in/i }).click();

  // Fill the login form (OpenIddict authorization endpoint)
  await page.getByLabel(/email/i).fill('admin@simplemodule.dev');
  await page.getByLabel(/password/i).fill('Admin123!');
  await page.getByRole('button', { name: /sign in|log in|submit/i }).click();

  // Wait for redirect back to the app after successful login
  await page.waitForURL('/');

  // Verify we're authenticated — dashboard should show user info
  await expect(page.locator('body')).toBeVisible();

  // Store auth state for reuse
  await page.context().storageState({ path: authFile });
});
```

> **Note:** The exact selectors for the login form depend on the rendered login page. The implementor should run `npx playwright test --headed --debug` to inspect the actual login form and adjust selectors if needed.

**Step 2: Create `tests/e2e/fixtures/base.ts`**

```typescript
import { test as base } from '@playwright/test';

export const test = base.extend({
  storageState: async ({}, use) => {
    await use('auth/.auth/user.json');
  },
});

export { expect } from '@playwright/test';
```

**Step 3: Create the auth storage directory**

Run: `mkdir -p tests/e2e/auth/.auth`

**Step 4: Commit**

```bash
git add tests/e2e/tests/auth/auth.setup.ts tests/e2e/fixtures/base.ts
git commit -m "feat(e2e): add auth setup and base test fixture"
```

---

### Task 4: Page Object Models

**Files:**
- Create: `tests/e2e/pages/dashboard.page.ts`
- Create: `tests/e2e/pages/products/browse.page.ts`
- Create: `tests/e2e/pages/products/create.page.ts`
- Create: `tests/e2e/pages/products/edit.page.ts`
- Create: `tests/e2e/pages/products/manage.page.ts`
- Create: `tests/e2e/pages/orders/list.page.ts`
- Create: `tests/e2e/pages/orders/create.page.ts`
- Create: `tests/e2e/pages/orders/edit.page.ts`

**Step 1: Create `tests/e2e/pages/dashboard.page.ts`**

```typescript
import type { Page } from '@playwright/test';

export class DashboardPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/');
  }

  get heading() {
    return this.page.getByRole('heading').first();
  }
}
```

**Step 2: Create `tests/e2e/pages/products/browse.page.ts`**

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

**Step 3: Create `tests/e2e/pages/products/create.page.ts`**

```typescript
import type { Page } from '@playwright/test';

export class ProductsCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create/i });
  }

  get nameInput() {
    return this.page.getByLabel(/name/i);
  }

  get priceInput() {
    return this.page.getByLabel(/price/i);
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /save|create|submit/i });
  }

  async createProduct(name: string, price: string) {
    await this.nameInput.fill(name);
    await this.priceInput.fill(price);
    await this.submitButton.click();
  }
}
```

**Step 4: Create `tests/e2e/pages/products/edit.page.ts`**

```typescript
import type { Page } from '@playwright/test';

export class ProductsEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit/i });
  }

  get nameInput() {
    return this.page.getByLabel(/name/i);
  }

  get priceInput() {
    return this.page.getByLabel(/price/i);
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /save|update|submit/i });
  }

  async updateProduct(name: string, price: string) {
    await this.nameInput.clear();
    await this.nameInput.fill(name);
    await this.priceInput.clear();
    await this.priceInput.fill(price);
    await this.submitButton.click();
  }
}
```

**Step 5: Create `tests/e2e/pages/products/manage.page.ts`**

```typescript
import type { Page } from '@playwright/test';

export class ProductsManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/manage');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /manage|products/i });
  }

  editLink(productName: string) {
    return this.page
      .getByRole('row', { name: new RegExp(productName, 'i') })
      .getByRole('link', { name: /edit/i });
  }

  deleteButton(productName: string) {
    return this.page
      .getByRole('row', { name: new RegExp(productName, 'i') })
      .getByRole('button', { name: /delete/i });
  }

  productRow(name: string) {
    return this.page.getByRole('row', { name: new RegExp(name, 'i') });
  }
}
```

**Step 6: Create `tests/e2e/pages/orders/list.page.ts`**

```typescript
import type { Page } from '@playwright/test';

export class OrdersListPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/orders/list');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /orders/i });
  }
}
```

**Step 7: Create `tests/e2e/pages/orders/create.page.ts`**

```typescript
import type { Page } from '@playwright/test';

export class OrdersCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/orders/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create/i });
  }
}
```

**Step 8: Create `tests/e2e/pages/orders/edit.page.ts`**

```typescript
import type { Page } from '@playwright/test';

export class OrdersEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit/i });
  }
}
```

**Step 9: Commit**

```bash
git add tests/e2e/pages/
git commit -m "feat(e2e): add Page Object Models for all modules"
```

---

### Task 5: Smoke tests

**Files:**
- Create: `tests/e2e/tests/smoke/dashboard.spec.ts`
- Create: `tests/e2e/tests/smoke/products.spec.ts`
- Create: `tests/e2e/tests/smoke/orders.spec.ts`

**Step 1: Create `tests/e2e/tests/smoke/dashboard.spec.ts`**

```typescript
import { test, expect } from '../../fixtures/base';
import { DashboardPage } from '../../pages/dashboard.page';

test.describe('Dashboard', () => {
  test('home page loads', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();
  });
});
```

**Step 2: Create `tests/e2e/tests/smoke/products.spec.ts`**

```typescript
import { test, expect } from '../../fixtures/base';
import { ProductsBrowsePage } from '../../pages/products/browse.page';
import { ProductsCreatePage } from '../../pages/products/create.page';
import { ProductsManagePage } from '../../pages/products/manage.page';

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

  test('manage page loads', async ({ page }) => {
    const manage = new ProductsManagePage(page);
    await manage.goto();
    await expect(manage.heading).toBeVisible();
  });
});
```

**Step 3: Create `tests/e2e/tests/smoke/orders.spec.ts`**

```typescript
import { test, expect } from '../../fixtures/base';
import { OrdersListPage } from '../../pages/orders/list.page';
import { OrdersCreatePage } from '../../pages/orders/create.page';

test.describe('Orders pages', () => {
  test('list page loads', async ({ page }) => {
    const list = new OrdersListPage(page);
    await list.goto();
    await expect(list.heading).toBeVisible();
  });

  test('create page loads', async ({ page }) => {
    const create = new OrdersCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });
});
```

**Step 4: Run smoke tests to verify they pass**

Run: `npm run test:smoke -w tests/e2e`

Expected: All smoke tests pass (pages load and headings are visible).

**Step 5: Commit**

```bash
git add tests/e2e/tests/smoke/
git commit -m "feat(e2e): add smoke tests for all pages"
```

---

### Task 6: Products CRUD flow test

**Files:**
- Create: `tests/e2e/tests/flows/products-crud.spec.ts`

**Step 1: Create `tests/e2e/tests/flows/products-crud.spec.ts`**

```typescript
import { test, expect } from '../../fixtures/base';
import { ProductsCreatePage } from '../../pages/products/create.page';
import { ProductsBrowsePage } from '../../pages/products/browse.page';
import { ProductsManagePage } from '../../pages/products/manage.page';
import { ProductsEditPage } from '../../pages/products/edit.page';

test.describe('Products CRUD', () => {
  test('create, verify, edit, and delete a product', async ({ page }) => {
    const createPage = new ProductsCreatePage(page);
    const browsePage = new ProductsBrowsePage(page);
    const managePage = new ProductsManagePage(page);
    const editPage = new ProductsEditPage(page);

    // Create a product
    await createPage.goto();
    await createPage.createProduct('E2E Test Product', '49.99');

    // Verify it appears on browse page
    await browsePage.goto();
    await expect(browsePage.productByName('E2E Test Product')).toBeVisible();

    // Edit the product via manage page
    await managePage.goto();
    await managePage.editLink('E2E Test Product').click();
    await editPage.updateProduct('E2E Updated Product', '59.99');

    // Verify the update on browse page
    await browsePage.goto();
    await expect(browsePage.productByName('E2E Updated Product')).toBeVisible();
    await expect(browsePage.productByName('E2E Test Product')).not.toBeVisible();

    // Delete the product via manage page
    await managePage.goto();
    await managePage.deleteButton('E2E Updated Product').click();

    // Verify it's gone
    await expect(managePage.productRow('E2E Updated Product')).not.toBeVisible();
  });
});
```

**Step 2: Run the flow test**

Run: `npm run test:flows -w tests/e2e`

Expected: PASS — full CRUD lifecycle works end-to-end.

**Step 3: Commit**

```bash
git add tests/e2e/tests/flows/products-crud.spec.ts
git commit -m "feat(e2e): add Products CRUD flow test"
```

---

### Task 7: Orders CRUD flow test

**Files:**
- Create: `tests/e2e/tests/flows/orders-crud.spec.ts`

**Step 1: Create `tests/e2e/tests/flows/orders-crud.spec.ts`**

> **Note:** The exact form fields depend on the Orders module's Create/Edit views. The implementor should inspect the Orders create page (`/orders/create`) in headed mode (`npx playwright test --headed --debug`) and adjust selectors to match the actual form fields (e.g., customer name, order items, total).

```typescript
import { test, expect } from '../../fixtures/base';
import { OrdersListPage } from '../../pages/orders/list.page';
import { OrdersCreatePage } from '../../pages/orders/create.page';

test.describe('Orders CRUD', () => {
  test('create an order and verify it appears in the list', async ({ page }) => {
    const createPage = new OrdersCreatePage(page);
    const listPage = new OrdersListPage(page);

    // Navigate to create page
    await createPage.goto();
    await expect(createPage.heading).toBeVisible();

    // TODO: Fill in order form fields — inspect the actual form to determine fields
    // Example:
    // await page.getByLabel(/customer/i).fill('E2E Test Customer');
    // await page.getByRole('button', { name: /save|create|submit/i }).click();

    // Verify it appears in the list
    // await listPage.goto();
    // await expect(page.getByText('E2E Test Customer')).toBeVisible();
  });
});
```

**Step 2: Run the test (initially may be placeholder)**

Run: `npm run test:flows -w tests/e2e`

**Step 3: Commit**

```bash
git add tests/e2e/tests/flows/orders-crud.spec.ts
git commit -m "feat(e2e): add Orders CRUD flow test scaffold"
```

---

### Task 8: Run full suite and fix selector issues

**Step 1: Start the server in headed debug mode**

Run: `npx -w tests/e2e playwright test --headed --debug`

This opens the browser with step-by-step execution so you can inspect the actual DOM and verify all selectors match.

**Step 2: Fix any selector mismatches**

Common issues:
- Login form labels/buttons may differ from assumed names — update `auth.setup.ts`
- Page headings may use different text — update smoke tests and POM `heading` selectors
- Table rows in manage page may not use `<tr>` — update `getByRole('row')` to match actual markup
- Form labels may not match assumed patterns — update POM input selectors

**Step 3: Run the full suite**

Run: `npm run test -w tests/e2e`

Expected: All tests pass.

**Step 4: Commit any fixes**

```bash
git add tests/e2e/
git commit -m "fix(e2e): adjust selectors to match actual UI"
```

---

### Task 9: Verify CI readiness

**Step 1: Test with CI environment variable**

Run: `CI=true npm run test -w tests/e2e`

This verifies:
- `forbidOnly` is active
- Retries are set to 2
- Workers limited to 1
- Cross-browser projects (firefox, webkit) would activate (if browsers installed)

**Step 2: Verify HTML report generation**

Run: `npm run report -w tests/e2e`

Expected: Opens the Playwright HTML report in a browser.

**Step 3: Final commit**

```bash
git add tests/e2e/
git commit -m "chore(e2e): verify CI configuration and reporting"
```

---

## Summary

| Task | Description | Key Files |
|------|------------|-----------|
| 1 | Scaffold E2E project | `tests/e2e/package.json`, root `package.json`, `biome.json` |
| 2 | Playwright config | `tests/e2e/playwright.config.ts` |
| 3 | Auth setup + fixture | `tests/e2e/tests/auth/auth.setup.ts`, `tests/e2e/fixtures/base.ts` |
| 4 | Page Object Models | `tests/e2e/pages/**/*.page.ts` |
| 5 | Smoke tests | `tests/e2e/tests/smoke/*.spec.ts` |
| 6 | Products CRUD flow | `tests/e2e/tests/flows/products-crud.spec.ts` |
| 7 | Orders CRUD flow | `tests/e2e/tests/flows/orders-crud.spec.ts` |
| 8 | Selector fixes | Various — based on headed debug run |
| 9 | CI verification | Verify env flags and reporting |
