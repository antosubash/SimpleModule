import { readFileSync } from 'node:fs';
import path from 'node:path';
import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

// DatasetStatus enum (modules/Datasets/src/SimpleModule.Datasets.Contracts/DatasetStatus.cs)
const STATUS_READY = 2;
const STATUS_FAILED = 3;

const fixturePath = path.resolve(__dirname, '../../fixtures/data/point.geojson');
const fixtureName = 'point.geojson';

test.describe('Datasets CRUD', () => {
  test('upload, poll ready, view, features, download, delete', async ({ page, request }) => {
    const datasetName = `e2e-ds-${faker.string.alphanumeric(6)}`;

    // Upload via multipart POST
    const fileBuffer = readFileSync(fixturePath);
    const uploadRes = await request.post('/api/datasets/', {
      multipart: {
        file: {
          name: fixtureName,
          mimeType: 'application/geo+json',
          buffer: fileBuffer,
        },
        name: datasetName,
      },
    });
    expect(uploadRes.ok()).toBeTruthy();
    const dataset = await uploadRes.json();
    expect(dataset.id).toBeTruthy();

    // Poll until Ready (or fail)
    let ready = false;
    for (let i = 0; i < 20; i++) {
      const getRes = await request.get(`/api/datasets/${dataset.id}`);
      expect(getRes.ok()).toBeTruthy();
      const dto = await getRes.json();
      if (dto.status === STATUS_READY) {
        ready = true;
        break;
      }
      expect(dto.status).not.toBe(STATUS_FAILED);
      await new Promise((r) => setTimeout(r, 500));
    }
    expect(ready).toBeTruthy();

    // Browse page shows the dataset name
    await page.goto('/datasets');
    await expect(page.getByText(datasetName)).toBeVisible();

    // Detail page loads
    await page.goto(`/datasets/${dataset.id}`);
    await expect(page.getByRole('heading', { name: datasetName })).toBeVisible();

    // Features endpoint returns the single point
    const featuresRes = await request.get(`/api/datasets/${dataset.id}/features`);
    expect(featuresRes.ok()).toBeTruthy();
    const featureCollection = (await featuresRes.json()) as { features: unknown[] };
    expect(featureCollection.features.length).toBeGreaterThanOrEqual(1);

    // Download original succeeds
    const downloadRes = await request.get(`/api/datasets/${dataset.id}/download?variant=original`);
    expect(downloadRes.ok()).toBeTruthy();

    // Delete
    const deleteRes = await request.delete(`/api/datasets/${dataset.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // Verify gone via list
    const listRes = await request.get('/api/datasets/');
    expect(listRes.ok()).toBeTruthy();
    const list = (await listRes.json()) as Array<{ id: string }>;
    expect(list.find((d) => d.id === dataset.id)).toBeFalsy();
  });
});
