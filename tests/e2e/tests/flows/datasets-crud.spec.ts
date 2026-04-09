import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { DatasetsBrowsePage } from '../../pages/datasets/browse.page';

// A minimal valid GeoJSON FeatureCollection — two points.
const sampleGeoJson = JSON.stringify({
  type: 'FeatureCollection',
  features: [
    {
      type: 'Feature',
      properties: { name: 'Alpha' },
      geometry: { type: 'Point', coordinates: [0, 0] },
    },
    {
      type: 'Feature',
      properties: { name: 'Beta' },
      geometry: { type: 'Point', coordinates: [10, 20] },
    },
  ],
});

test.describe('Datasets CRUD flows', () => {
  test('upload, list, view and delete a GeoJSON dataset via API', async ({ page, request }) => {
    const filename = `e2e-${faker.string.alphanumeric(8)}.geojson`;

    // API: upload a dataset via multipart/form-data
    const uploadRes = await request.post('/api/datasets', {
      multipart: {
        file: {
          name: filename,
          mimeType: 'application/geo+json',
          buffer: Buffer.from(sampleGeoJson, 'utf-8'),
        },
      },
    });
    expect(uploadRes.ok()).toBeTruthy();
    const created = await uploadRes.json();
    expect(created.id).toBeTruthy();
    expect(created.originalFileName).toBe(filename);
    // Format 1 = GeoJson
    expect(created.format).toBe(1);

    // API: verify in list
    const listRes = await request.get('/api/datasets');
    expect(listRes.ok()).toBeTruthy();
    const list = await listRes.json();
    expect(list.some((d: { id: string }) => d.id === created.id)).toBeTruthy();

    // API: fetch by id
    const getRes = await request.get(`/api/datasets/${created.id}`);
    expect(getRes.ok()).toBeTruthy();
    const got = await getRes.json();
    expect(got.id).toBe(created.id);

    // UI: browse page shows the dataset name
    const browse = new DatasetsBrowsePage(page);
    await browse.goto();
    await expect(page.getByText(created.name)).toBeVisible();

    // API: delete
    const deleteRes = await request.delete(`/api/datasets/${created.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // API: verify gone
    const afterRes = await request.get(`/api/datasets/${created.id}`);
    expect(afterRes.status()).toBe(404);
  });

  test('upload via UI Choose-file input and redirects after processing', async ({
    page,
    request,
  }) => {
    const filename = `ui-upload-${faker.string.alphanumeric(6)}.geojson`;

    await page.goto('/datasets/upload');
    await expect(page.getByRole('heading', { name: /upload gis dataset/i })).toBeVisible();

    // The page uses a hidden <input type="file"> triggered by the button.
    // setInputFiles bypasses the click-to-open dialog.
    const fileInput = page.locator('input[type="file"]');
    await fileInput.setInputFiles({
      name: filename,
      mimeType: 'application/geo+json',
      buffer: Buffer.from(sampleGeoJson, 'utf-8'),
    });

    // Wait for the upload POST to settle
    const response = await page.waitForResponse(
      (resp) => resp.url().endsWith('/api/datasets') && resp.request().method() === 'POST',
      { timeout: 10_000 },
    );
    expect(response.ok()).toBeTruthy();
    const created = await response.json();

    // Cleanup via API
    await request.delete(`/api/datasets/${created.id}`);
  });

  test('upload rejects unsupported format', async ({ request }) => {
    const uploadRes = await request.post('/api/datasets', {
      multipart: {
        file: {
          name: 'readme.md',
          mimeType: 'text/markdown',
          buffer: Buffer.from('# hello', 'utf-8'),
        },
      },
    });
    expect(uploadRes.status()).toBe(400);
  });

  test('get non-existent dataset returns 404', async ({ request }) => {
    const bogus = '00000000-0000-0000-0000-000000000000';
    const res = await request.get(`/api/datasets/${bogus}`);
    expect(res.status()).toBe(404);
  });
});
