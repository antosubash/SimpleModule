import { expect } from '@playwright/test';
import { test } from '../../fixtures/base';
import { FileStorageBrowsePage } from '../../pages/filestorage/browse.page';

test.describe('FileStorage smoke', () => {
  test('browse page loads with empty state', async ({ page }) => {
    const browse = new FileStorageBrowsePage(page);
    await page.goto('/files');

    await expect(browse.uploadButton).toBeVisible();
    await expect(browse.emptyTitle).toBeVisible();
    await expect(browse.emptyDescription).toBeVisible();
  });

  test('sidebar shows Files link', async ({ page }) => {
    await page.goto('/');
    const filesLink = page.getByRole('link', { name: 'Files' });
    await expect(filesLink).toBeVisible();
    await expect(filesLink).toHaveAttribute('href', '/files');
  });

  test('API endpoint returns empty list', async ({ request }) => {
    const response = await request.get('/api/files');
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data).toEqual([]);
  });

  test('folders endpoint returns empty list', async ({ request }) => {
    const response = await request.get('/api/files/folders');
    expect(response.status()).toBe(200);

    const data = await response.json();
    expect(data).toEqual([]);
  });

  test.describe('unauthenticated', () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test('browse page redirects to login', async ({ page }) => {
      const response = await page.goto('/files');
      expect(response?.url()).toContain('/Account/Login');
    });

    test('API endpoint returns 401', async ({ request }) => {
      const response = await request.get('/api/files', {
        maxRedirects: 0,
      });
      expect(response.status()).toBe(401);
    });
  });
});
