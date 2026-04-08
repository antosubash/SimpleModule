import { expect, test } from '../../fixtures/base';

test.describe('Email pages', () => {
  test('templates page loads', async ({ page }) => {
    await page.goto('/email/templates');
    await expect(page.getByRole('heading', { name: /email templates/i })).toBeVisible();
  });

  test('create template page loads', async ({ page }) => {
    await page.goto('/email/templates/create');
    await expect(page.getByRole('heading', { name: /create template/i })).toBeVisible();
  });

  test('history page loads', async ({ page }) => {
    await page.goto('/email/history');
    await expect(page.getByRole('heading', { name: /email history/i })).toBeVisible();
  });

  test('dashboard page loads', async ({ page }) => {
    await page.goto('/email/dashboard');
    await expect(page.getByRole('heading', { name: /email dashboard/i })).toBeVisible();
  });

  test('settings page loads', async ({ page }) => {
    await page.goto('/email/settings');
    await expect(page.getByRole('heading', { name: /email settings/i })).toBeVisible();
  });
});
