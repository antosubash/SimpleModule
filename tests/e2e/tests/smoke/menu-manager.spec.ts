import { expect, test } from '../../fixtures/base';
import { MenuManagerPage } from '../../pages/settings/menu-manager.page';

test.describe('Menu Manager - Smoke', () => {
  test('page loads for authenticated user', async ({ page }) => {
    const menuManager = new MenuManagerPage(page);
    await menuManager.goto();
    await expect(menuManager.heading).toBeVisible();
  });

  test('shows add item button', async ({ page }) => {
    const menuManager = new MenuManagerPage(page);
    await menuManager.goto();
    await expect(menuManager.addItemButton).toBeVisible();
  });

  test('available pages API returns data', async ({ request }) => {
    const response = await request.get('/api/settings/menus/available-pages');
    expect(response.ok()).toBeTruthy();
    const body = await response.json();
    expect(Array.isArray(body)).toBeTruthy();
    expect(body.length).toBeGreaterThan(0);
  });

  test('menus API returns array', async ({ request }) => {
    const response = await request.get('/api/settings/menus');
    expect(response.ok()).toBeTruthy();
    const body = await response.json();
    expect(Array.isArray(body)).toBeTruthy();
  });
});
