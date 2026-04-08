import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

test.describe('Map basemaps CRUD', () => {
  test('create, list, delete a basemap via API', async ({ page, request }) => {
    const name = `e2e-bm-${faker.string.alphanumeric(6)}`;

    const createRes = await request.post('/api/map/basemaps', {
      data: {
        name,
        description: 'e2e test basemap',
        styleUrl: 'https://demotiles.maplibre.org/style.json',
        attribution: '© MapLibre',
        thumbnailUrl: null,
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const basemap = await createRes.json();
    expect(basemap.id).toBeTruthy();

    // Verify via list endpoint
    const listRes = await request.get('/api/map/basemaps');
    expect(listRes.ok()).toBeTruthy();
    const list = (await listRes.json()) as Array<{ id: string; name: string }>;
    expect(list.find((b) => b.id === basemap.id)?.name).toBe(name);

    // Verify on /map/layers
    await page.goto('/map/layers');
    await expect(page.getByText(name)).toBeVisible();

    // Delete
    const deleteRes = await request.delete(`/api/map/basemaps/${basemap.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // Verify gone via API
    const afterRes = await request.get('/api/map/basemaps');
    const after = (await afterRes.json()) as Array<{ id: string }>;
    expect(after.find((b) => b.id === basemap.id)).toBeFalsy();
  });
});
