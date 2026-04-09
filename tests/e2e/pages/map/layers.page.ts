import type { Page } from '@playwright/test';

export class MapLayersPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/map/layers');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /map catalog/i });
  }

  get addLayerSourceButton() {
    return this.page.getByRole('button', { name: /add layer source/i });
  }

  get addBasemapButton() {
    return this.page.getByRole('button', { name: /add basemap/i });
  }

  get layerSourceCards() {
    return this.page.locator('[data-testid="layer-source-card"]');
  }

  get basemapCards() {
    return this.page.locator('[data-testid="basemap-card"]');
  }

  layerSourceByName(name: string) {
    return this.page.locator('[data-testid="layer-source-card"]').filter({ hasText: name });
  }

  basemapByName(name: string) {
    return this.page.locator('[data-testid="basemap-card"]').filter({ hasText: name });
  }

  // Dialog controls for layer source
  get layerNameInput() {
    return this.page.getByLabel('Name').first();
  }

  get layerUrlInput() {
    return this.page.getByLabel('URL');
  }

  get layerAttributionInput() {
    return this.page.getByLabel('Attribution').first();
  }

  // Dialog controls for basemap
  get basemapNameInput() {
    return this.page.getByLabel('Name').nth(1);
  }

  get basemapStyleUrlInput() {
    return this.page.getByLabel(/maplibre style url/i);
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /^save$/i });
  }
}
