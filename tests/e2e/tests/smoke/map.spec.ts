import { expect, test } from '../../fixtures/base';
import { MapBrowsePage } from '../../pages/map/browse.page';
import { MapLayersPage } from '../../pages/map/layers.page';

test.describe('Map pages', () => {
  test('browse page loads', async ({ page }) => {
    const browse = new MapBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });

  test('browse page shows manage-catalog and save actions', async ({ page }) => {
    const browse = new MapBrowsePage(page);
    await browse.goto();
    await expect(browse.manageCatalogButton).toBeVisible();
    await expect(browse.saveButton).toBeVisible();
  });

  test('browse page shows configuration side panel', async ({ page }) => {
    const browse = new MapBrowsePage(page);
    await browse.goto();
    await expect(browse.sidePanel).toBeVisible();
  });

  test('layers page loads', async ({ page }) => {
    const layers = new MapLayersPage(page);
    await layers.goto();
    await expect(layers.heading).toBeVisible();
  });

  test('layers page has add-source and add-basemap actions', async ({ page }) => {
    const layers = new MapLayersPage(page);
    await layers.goto();
    await expect(layers.addLayerSourceButton).toBeVisible();
    await expect(layers.addBasemapButton).toBeVisible();
  });
});
