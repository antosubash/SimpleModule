import { expect, test } from '../../fixtures/base';

test.describe('Datasets pages', () => {
  test('browse page loads', async ({ page }) => {
    await page.goto('/datasets');
    await expect(page.getByRole('heading', { name: /datasets/i })).toBeVisible();
  });

  test('upload page loads', async ({ page }) => {
    await page.goto('/datasets/upload');
    await expect(page.getByRole('heading', { name: /upload gis dataset/i })).toBeVisible();
  });
});
