import { expect, test } from '../../fixtures/base';

test.describe('Permission System', () => {
  test.describe('authenticated admin user', () => {
    // These tests use the default auth state (admin user)

    test('can access products API', async ({ page }) => {
      const response = await page.request.get('https://localhost:5001/api/products');
      expect(response.status()).toBe(200);
    });

    test('can access orders API', async ({ page }) => {
      const response = await page.request.get('https://localhost:5001/api/orders');
      expect(response.status()).toBe(200);
    });

    test('can access admin users page', async ({ page }) => {
      await page.goto('/admin/users');
      await expect(page.getByRole('heading', { name: /users/i })).toBeVisible();
    });

    test('can access admin roles page', async ({ page }) => {
      await page.goto('/admin/roles');
      await expect(page.getByRole('heading', { name: /roles/i })).toBeVisible();
    });

    test('can access products manage page', async ({ page }) => {
      await page.goto('/products/manage');
      await expect(page.getByRole('heading', { name: /manage|products/i })).toBeVisible();
    });
  });

  test.describe('unauthenticated user', () => {
    // Clear auth state for these tests
    test.use({ storageState: { cookies: [], origins: [] } });

    test('products API rejects unauthenticated request', async ({ request }) => {
      const response = await request.get('https://localhost:5001/api/products', {
        maxRedirects: 0,
      });
      // Identity cookie scheme returns 302 redirect to login for unauthenticated requests
      expect(response.status()).toBe(302);
    });

    test('orders API rejects unauthenticated request', async ({ request }) => {
      const response = await request.get('https://localhost:5001/api/orders', {
        maxRedirects: 0,
      });
      expect(response.status()).toBe(302);
    });

    test('can access public browse page', async ({ page }) => {
      await page.goto('/products');
      // Should NOT redirect to login — this page is AllowAnonymous
      await expect(page.getByRole('heading', { name: /products/i })).toBeVisible();
    });

    test('can access home page', async ({ page }) => {
      await page.goto('/');
      // Home page is AllowAnonymous — should load without redirect
      await expect(page.locator('body')).toBeVisible();
      // Should not be on login page
      expect(page.url()).not.toContain('/Account/Login');
    });

    test('protected page redirects to login', async ({ page }) => {
      await page.goto('/products/manage');
      // Should redirect to login — verify we're not on the manage page
      expect(page.url()).toContain('/Account/Login');
    });

    test('admin page redirects to login', async ({ page }) => {
      await page.goto('/admin/users');
      // Should redirect to login
      expect(page.url()).toContain('/Account/Login');
    });
  });
});
