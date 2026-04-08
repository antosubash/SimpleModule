import { expect, test } from '../../fixtures/base';

test.describe('Map pages', () => {
  test('browse page loads', async ({ page }) => {
    await page.goto('/map');
    await expect(page.getByRole('heading', { name: /maps/i })).toBeVisible();
  });

  test('layers page loads', async ({ page }) => {
    await page.goto('/map/layers');
    await expect(page.getByRole('heading', { name: /map catalog/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /add layer source/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /add basemap/i })).toBeVisible();
  });
});
