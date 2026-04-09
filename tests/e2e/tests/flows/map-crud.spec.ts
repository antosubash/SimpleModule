import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { MapBrowsePage } from '../../pages/map/browse.page';
import { MapLayersPage } from '../../pages/map/layers.page';

test.describe('Map CRUD flows', () => {
  test('create, list, and delete a layer source via API, verify in UI', async ({
    page,
    request,
  }) => {
    const sourceName = `e2e-layer-${faker.string.alphanumeric(8)}`;

    // API: create a layer source (type 3 = Xyz)
    const createRes = await request.post('/api/map/sources', {
      data: {
        name: sourceName,
        description: 'e2e layer source',
        type: 3,
        url: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
        attribution: 'e2e',
        metadata: {},
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.name).toBe(sourceName);

    // API: verify it's in the list
    const listRes = await request.get('/api/map/sources');
    expect(listRes.ok()).toBeTruthy();
    const sources = await listRes.json();
    expect(sources.some((s: { name: string }) => s.name === sourceName)).toBeTruthy();

    // UI: verify it renders on the layers page
    const layers = new MapLayersPage(page);
    await layers.goto();
    await expect(layers.layerSourceByName(sourceName)).toBeVisible();

    // API: delete it
    const deleteRes = await request.delete(`/api/map/sources/${created.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // API: verify it's gone
    const afterRes = await request.get('/api/map/sources');
    const after = await afterRes.json();
    expect(after.some((s: { name: string }) => s.name === sourceName)).toBeFalsy();
  });

  test('create, list, and delete a basemap via API, verify in UI', async ({ page, request }) => {
    const basemapName = `e2e-basemap-${faker.string.alphanumeric(8)}`;

    // API: create a basemap
    const createRes = await request.post('/api/map/basemaps', {
      data: {
        name: basemapName,
        description: 'e2e basemap',
        styleUrl: 'https://demotiles.maplibre.org/style.json',
        attribution: 'e2e',
        thumbnailUrl: null,
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.name).toBe(basemapName);

    // API: verify
    const listRes = await request.get('/api/map/basemaps');
    const list = await listRes.json();
    expect(list.some((b: { name: string }) => b.name === basemapName)).toBeTruthy();

    // UI: verify on layers page
    const layers = new MapLayersPage(page);
    await layers.goto();
    await expect(layers.basemapByName(basemapName)).toBeVisible();

    // API: delete
    const deleteRes = await request.delete(`/api/map/basemaps/${created.id}`);
    expect(deleteRes.ok()).toBeTruthy();
  });

  test('create, update, and delete a saved map via API', async ({ page, request }) => {
    const mapName = `e2e-map-${faker.string.alphanumeric(8)}`;
    const updatedName = `${mapName}-updated`;

    // API: create
    const createRes = await request.post('/api/map/maps', {
      data: {
        name: mapName,
        description: 'e2e map',
        centerLng: 0,
        centerLat: 0,
        zoom: 2,
        pitch: 0,
        bearing: 0,
        baseStyleUrl: 'https://demotiles.maplibre.org/style.json',
        layers: [],
        basemaps: [],
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.name).toBe(mapName);

    // API: verify appears in list
    const listRes = await request.get('/api/map/maps');
    const maps = await listRes.json();
    expect(maps.some((m: { name: string }) => m.name === mapName)).toBeTruthy();

    // UI: browse page shows the new map card
    const browse = new MapBrowsePage(page);
    await browse.goto();
    await expect(browse.mapCardByName(mapName)).toBeVisible();

    // API: update
    const updateRes = await request.put(`/api/map/maps/${created.id}`, {
      data: {
        name: updatedName,
        description: 'e2e map updated',
        centerLng: 1,
        centerLat: 2,
        zoom: 5,
        pitch: 0,
        bearing: 0,
        baseStyleUrl: 'https://demotiles.maplibre.org/style.json',
        layers: [],
        basemaps: [],
      },
    });
    expect(updateRes.ok()).toBeTruthy();

    // API: verify update
    const afterUpdateRes = await request.get(`/api/map/maps/${created.id}`);
    expect(afterUpdateRes.ok()).toBeTruthy();
    const updated = await afterUpdateRes.json();
    expect(updated.name).toBe(updatedName);
    expect(updated.zoom).toBe(5);

    // API: delete
    const deleteRes = await request.delete(`/api/map/maps/${created.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // API: verify gone
    const afterDeleteRes = await request.get('/api/map/maps');
    const afterDelete = await afterDeleteRes.json();
    expect(afterDelete.some((m: { id: string }) => m.id === created.id)).toBeFalsy();
  });

  test('add layer source via UI dialog', async ({ page, request }) => {
    const sourceName = `e2e-ui-layer-${faker.string.alphanumeric(6)}`;
    const layers = new MapLayersPage(page);
    await layers.goto();

    // Open dialog
    await layers.addLayerSourceButton.click();
    await expect(page.getByRole('dialog')).toBeVisible();

    // Fill required fields (Name, URL) — Type defaults to WMS
    await page.getByLabel('Name').first().fill(sourceName);
    await page.getByLabel('URL').fill('https://example.com/wms');

    // Submit form
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/map/sources') && resp.request().method() === 'POST',
      ),
      page.getByRole('button', { name: /^save$/i }).click(),
    ]);

    // Verify the new source appears
    await expect(layers.layerSourceByName(sourceName)).toBeVisible();

    // Cleanup via API: look it up and delete
    const listRes = await request.get('/api/map/sources');
    const sources = (await listRes.json()) as { id: string; name: string }[];
    const created = sources.find((s) => s.name === sourceName);
    if (created) {
      await request.delete(`/api/map/sources/${created.id}`);
    }
  });
});
