import { readFileSync } from 'node:fs';
import path from 'node:path';
import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

// DatasetStatus enum (modules/Datasets/src/SimpleModule.Datasets.Contracts/DatasetStatus.cs)
const STATUS_READY = 2;
const STATUS_FAILED = 3;

const fixturePath = path.resolve(__dirname, '../../fixtures/data/point.geojson');

test.describe('Datasets API', () => {
  test('list endpoint returns an array and respects upload name', async ({ request }) => {
    const name = `e2e-api-${faker.string.alphanumeric(6)}`;

    const uploadRes = await request.post('/api/datasets/', {
      multipart: {
        file: {
          name: 'point.geojson',
          mimeType: 'application/geo+json',
          buffer: readFileSync(fixturePath),
        },
        name,
      },
    });
    expect(uploadRes.ok()).toBeTruthy();
    const uploaded = await uploadRes.json();
    expect(uploaded.name).toBe(name);

    // Poll until processed (ready or failed)
    let finalStatus: number | undefined;
    for (let i = 0; i < 20; i++) {
      const getRes = await request.get(`/api/datasets/${uploaded.id}`);
      expect(getRes.ok()).toBeTruthy();
      const dto = await getRes.json();
      if (dto.status === STATUS_READY || dto.status === STATUS_FAILED) {
        finalStatus = dto.status;
        break;
      }
      await new Promise((r) => setTimeout(r, 500));
    }
    expect(finalStatus).toBe(STATUS_READY);

    const listRes = await request.get('/api/datasets/');
    expect(listRes.ok()).toBeTruthy();
    const list = (await listRes.json()) as Array<{ id: string; name: string }>;
    expect(Array.isArray(list)).toBeTruthy();
    expect(list.some((d) => d.id === uploaded.id && d.name === name)).toBeTruthy();

    // Cleanup
    const deleteRes = await request.delete(`/api/datasets/${uploaded.id}`);
    expect(deleteRes.ok()).toBeTruthy();
  });
});
