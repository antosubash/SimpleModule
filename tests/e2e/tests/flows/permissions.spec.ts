import { expect, test } from '../../fixtures/base';

test.describe('Permission System', () => {
  test.describe('authenticated admin user', () => {
    // These tests use the default auth state (admin user)

    test('can access admin users page', async ({ page }) => {
      await page.goto('/admin/users');
      await expect(page.getByRole('heading', { name: /users/i })).toBeVisible();
    });

    test('can access admin roles page', async ({ page }) => {
      await page.goto('/admin/roles');
      await expect(page.getByRole('heading', { name: /roles/i })).toBeVisible();
    });

    test('can access settings page', async ({ page }) => {
      await page.goto('/settings');
      await expect(page.getByRole('heading', { name: /settings/i })).toBeVisible();
    });
  });

  test.describe('unauthenticated user', () => {
    // Clear auth state for these tests
    test.use({ storageState: { cookies: [], origins: [] } });

    test('admin API rejects unauthenticated request', async ({ request }) => {
      const response = await request.get('/api/admin/users', {
        maxRedirects: 0,
      });
      // Identity cookie scheme returns 302 redirect to login for unauthenticated requests
      expect(response.status()).toBe(302);
    });

    test('can access home page', async ({ page }) => {
      await page.goto('/');
      // Home page is AllowAnonymous — should load without redirect
      await expect(page.locator('body')).toBeVisible();
      expect(page.url()).not.toContain('/Account/Login');
    });

    test('protected admin page redirects to login', async ({ page }) => {
      await page.goto('/admin/users');
      expect(page.url()).toContain('/Account/Login');
    });
  });
});
