import { faker } from '@faker-js/faker';
import { expect } from '@playwright/test';
import { test } from '../../fixtures/base';
import { FileStorageBrowsePage } from '../../pages/filestorage/browse.page';

test.describe
  .serial('FileStorage CRUD', () => {
    const createdIds: number[] = [];
    let testFileName: string;

    test.afterAll(async ({ request }) => {
      for (const id of createdIds) {
        await request.delete(`/api/files/${id}`).catch(() => {});
      }
    });

    test('upload a file via API', async ({ request }) => {
      testFileName = `test-${faker.string.alphanumeric(8)}.txt`;
      const content = faker.lorem.paragraph();

      const response = await request.post('/api/files', {
        multipart: {
          file: {
            name: testFileName,
            mimeType: 'text/plain',
            buffer: Buffer.from(content),
          },
        },
      });

      expect(response.status()).toBe(201);
      const data = await response.json();
      expect(data.fileName).toBe(testFileName);
      expect(data.contentType).toBe('text/plain');
      expect(data.size).toBeGreaterThan(0);
      expect(data.folder).toBeNull();
      createdIds.push(data.id);
    });

    test('file appears in browse UI', async ({ page }) => {
      const browse = new FileStorageBrowsePage(page);
      await page.goto('/files/browse');

      await expect(browse.fileRow(testFileName)).toBeVisible();
      await expect(browse.downloadButton(testFileName)).toBeVisible();
      await expect(browse.deleteButton(testFileName)).toBeVisible();
    });

    test('file appears in API list', async ({ request }) => {
      const response = await request.get('/api/files');
      expect(response.status()).toBe(200);

      const data = await response.json();
      const file = data.find((f: { fileName: string }) => f.fileName === testFileName);
      expect(file).toBeDefined();
      expect(file.contentType).toBe('text/plain');
    });

    test('download file via API', async ({ request }) => {
      const listResponse = await request.get('/api/files');
      const data = await listResponse.json();
      const file = data.find((f: { fileName: string }) => f.fileName === testFileName);

      const downloadResponse = await request.get(`/api/files/${file.id}/download`);
      expect(downloadResponse.status()).toBe(200);
      expect(downloadResponse.headers()['content-type']).toContain('text/plain');

      const body = await downloadResponse.text();
      expect(body.length).toBeGreaterThan(0);
    });

    test('upload file to folder via API', async ({ request }) => {
      const folderFileName = `folder-${faker.string.alphanumeric(8)}.txt`;

      const response = await request.post('/api/files?folder=docs', {
        multipart: {
          file: {
            name: folderFileName,
            mimeType: 'text/plain',
            buffer: Buffer.from('folder content'),
          },
        },
      });

      expect(response.status()).toBe(201);
      const data = await response.json();
      expect(data.folder).toBe('docs');
      expect(data.storagePath).toBe(`docs/${folderFileName}`);
      createdIds.push(data.id);
    });

    test('folder appears in browse UI', async ({ page }) => {
      const browse = new FileStorageBrowsePage(page);
      await page.goto('/files/browse');

      await expect(browse.folderRow('docs')).toBeVisible();
    });

    test('navigate into folder', async ({ page }) => {
      const browse = new FileStorageBrowsePage(page);
      await page.goto('/files/browse');

      await browse.folderRow('docs').click();
      await page.waitForURL(/folder=docs/);

      await expect(browse.parentRow).toBeVisible();
      await expect(browse.description).toContainText('1 file');
    });

    test('folders API returns docs folder', async ({ request }) => {
      const response = await request.get('/api/files/folders');
      expect(response.status()).toBe(200);

      const data = await response.json();
      expect(data).toContain('docs');
    });

    test('delete file via UI', async ({ page }) => {
      const browse = new FileStorageBrowsePage(page);
      await page.goto('/files/browse');

      await browse.deleteButton(testFileName).click();
      await expect(browse.deleteDialogTitle).toBeVisible();
      await browse.deleteConfirmButton.click();

      await page.waitForLoadState('networkidle');
      await expect(browse.fileRow(testFileName)).not.toBeVisible();

      // Remove from cleanup list since it's already deleted
      createdIds.shift();
    });

    test('deleted file no longer in API', async ({ request }) => {
      const response = await request.get('/api/files');
      const data = await response.json();
      const file = data.find((f: { fileName: string }) => f.fileName === testFileName);
      expect(file).toBeUndefined();
    });
  });

test.describe('FileStorage permissions', () => {
  test.describe('unauthenticated', () => {
    test.use({ storageState: { cookies: [], origins: [] } });

    test('upload is rejected', async ({ request }) => {
      const response = await request.post('/api/files', {
        maxRedirects: 0,
        multipart: {
          file: {
            name: 'test.txt',
            mimeType: 'text/plain',
            buffer: Buffer.from('test'),
          },
        },
      });
      expect(response.status()).toBe(302);
    });

    test('delete is rejected', async ({ request }) => {
      const response = await request.delete('/api/files/1', {
        maxRedirects: 0,
      });
      expect(response.status()).toBe(302);
    });
  });
});
