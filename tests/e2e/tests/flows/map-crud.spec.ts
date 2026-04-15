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

  test('get and update the singleton default map via API', async ({ page, request }) => {
    // API: read the current default map — it's seeded at module startup
    // and upserted in place, there is no create or delete.
    const getRes = await request.get('/api/map/default');
    expect(getRes.ok()).toBeTruthy();
    const current = await getRes.json();
    expect(current).toBeTruthy();

    // Pick a fresh viewport so the assertion below can tell the upsert ran.
    const newZoom = (current.zoom ?? 0) + 1;
    const updateRes = await request.put('/api/map/default', {
      data: {
        centerLng: 10,
        centerLat: 20,
        zoom: newZoom,
        pitch: 0,
        bearing: 0,
        baseStyleUrl: 'https://demotiles.maplibre.org/style.json',
        layers: [],
        basemaps: [],
      },
    });
    expect(updateRes.ok()).toBeTruthy();

    // API: verify the upsert landed
    const afterRes = await request.get('/api/map/default');
    expect(afterRes.ok()).toBeTruthy();
    const after = await afterRes.json();
    expect(after.zoom).toBe(newZoom);
    expect(after.centerLng).toBeCloseTo(10, 5);
    expect(after.centerLat).toBeCloseTo(20, 5);

    // UI: browse page renders the default map shell
    const browse = new MapBrowsePage(page);
    await browse.goto();
    await expect(browse.layersToggle).toBeVisible();
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
