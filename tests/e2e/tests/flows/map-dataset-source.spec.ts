import { readFileSync } from 'node:fs';
import path from 'node:path';
import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

// DatasetStatus.Ready = 2, LayerSourceType.Dataset = 8
const STATUS_READY = 2;
const STATUS_FAILED = 3;
const DATASET_SOURCE_TYPE = 8;

const fixturePath = path.resolve(__dirname, '../../fixtures/data/point.geojson');

test.describe('Map dataset-backed layer sources', () => {
  test('upload dataset, create layer source from it, delete both', async ({ request }) => {
    const datasetName = `e2e-mds-${faker.string.alphanumeric(6)}`;
    const sourceName = `e2e-msrc-${faker.string.alphanumeric(6)}`;

    // 1. Upload a dataset
    const uploadRes = await request.post('/api/datasets/', {
      multipart: {
        file: {
          name: 'point.geojson',
          mimeType: 'application/geo+json',
          buffer: readFileSync(fixturePath),
        },
        name: datasetName,
      },
    });
    expect(uploadRes.ok()).toBeTruthy();
    const dataset = await uploadRes.json();

    // 2. Wait for processing to finish
    let ready = false;
    for (let i = 0; i < 20; i++) {
      const dto = await (await request.get(`/api/datasets/${dataset.id}`)).json();
      if (dto.status === STATUS_READY) {
        ready = true;
        break;
      }
      expect(dto.status).not.toBe(STATUS_FAILED);
      await new Promise((r) => setTimeout(r, 500));
    }
    expect(ready).toBeTruthy();

    // 3. Create a layer source pointing at the dataset
    const createRes = await request.post('/api/map/sources/from-dataset', {
      data: {
        datasetId: dataset.id,
        name: sourceName,
        description: 'e2e dataset-backed source',
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const source = await createRes.json();
    expect(source.id).toBeTruthy();
    expect(source.name).toBe(sourceName);
    expect(source.type).toBe(DATASET_SOURCE_TYPE);

    // 4. Verify it shows up in the list
    const listRes = await request.get('/api/map/sources');
    expect(listRes.ok()).toBeTruthy();
    const sources = (await listRes.json()) as Array<{ id: string; name: string }>;
    expect(sources.some((s) => s.id === source.id && s.name === sourceName)).toBeTruthy();

    // 5. Cleanup
    const deleteSourceRes = await request.delete(`/api/map/sources/${source.id}`);
    expect(deleteSourceRes.ok()).toBeTruthy();

    const deleteDatasetRes = await request.delete(`/api/datasets/${dataset.id}`);
    expect(deleteDatasetRes.ok()).toBeTruthy();
  });
});
