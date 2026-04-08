import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

// LayerSourceType.Xyz = 3 (modules/Map/src/SimpleModule.Map.Contracts/LayerSourceType.cs)
const XYZ = 3;

test.describe('Map CRUD', () => {
  test('create layer source, saved map, view, edit, delete', async ({ page, request }) => {
    const sourceName = `e2e-src-${faker.string.alphanumeric(6)}`;
    const mapName = `e2e-map-${faker.string.alphanumeric(6)}`;
    const updatedName = `${mapName}-upd`;

    // Create a layer source via API
    const sourceRes = await request.post('/api/map/sources', {
      data: {
        name: sourceName,
        description: 'e2e test source',
        type: XYZ,
        url: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
        attribution: '© OSM',
        metadata: {},
      },
    });
    expect(sourceRes.ok()).toBeTruthy();
    const source = await sourceRes.json();
    expect(source.id).toBeTruthy();

    // Verify it appears on /map/layers
    await page.goto('/map/layers');
    await expect(page.getByText(sourceName)).toBeVisible();

    // Create a saved map that references it
    const createMapRes = await request.post('/api/map/maps', {
      data: {
        name: mapName,
        description: 'created by e2e',
        centerLng: 0,
        centerLat: 0,
        zoom: 2,
        pitch: 0,
        bearing: 0,
        baseStyleUrl: 'https://demotiles.maplibre.org/style.json',
        layers: [
          {
            layerSourceId: source.id,
            order: 0,
            visible: true,
            opacity: 1,
            styleOverrides: {},
          },
        ],
        basemaps: [],
      },
    });
    expect(createMapRes.ok()).toBeTruthy();
    const savedMap = await createMapRes.json();
    expect(savedMap.id).toBeTruthy();

    // UI: verify the saved map appears on /map
    await page.goto('/map');
    await expect(page.getByText(mapName)).toBeVisible();

    // View page loads
    await page.goto(`/map/${savedMap.id}`);
    await expect(page.getByRole('heading', { name: mapName })).toBeVisible();

    // Edit page loads
    await page.goto(`/map/${savedMap.id}/edit`);
    await expect(page.getByRole('heading', { name: /edit map/i })).toBeVisible();

    // Update the map via API (PUT)
    const updateRes = await request.put(`/api/map/maps/${savedMap.id}`, {
      data: {
        name: updatedName,
        description: 'updated by e2e',
        centerLng: 10,
        centerLat: 20,
        zoom: 5,
        pitch: 0,
        bearing: 0,
        baseStyleUrl: 'https://demotiles.maplibre.org/style.json',
        layers: [],
        basemaps: [],
      },
    });
    expect(updateRes.ok()).toBeTruthy();

    // Verify the update via API
    const getRes = await request.get(`/api/map/maps/${savedMap.id}`);
    expect(getRes.ok()).toBeTruthy();
    const updated = await getRes.json();
    expect(updated.name).toBe(updatedName);
    expect(updated.zoom).toBe(5);

    // Delete the map
    const deleteMapRes = await request.delete(`/api/map/maps/${savedMap.id}`);
    expect(deleteMapRes.ok()).toBeTruthy();

    // Delete the layer source
    const deleteSourceRes = await request.delete(`/api/map/sources/${source.id}`);
    expect(deleteSourceRes.ok()).toBeTruthy();

    // Verify gone from UI
    await page.goto('/map');
    await expect(page.getByText(updatedName)).not.toBeVisible();
    await page.goto('/map/layers');
    await expect(page.getByText(sourceName)).not.toBeVisible();
  });
});
