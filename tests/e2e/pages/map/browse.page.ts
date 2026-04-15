import type { Page } from '@playwright/test';

export class MapBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/map');
  }

  get layersToggle() {
    return this.page.getByTestId('layers-toggle');
  }

  get basemapsToggle() {
    return this.page.getByTestId('basemaps-toggle');
  }

  get layersPanel() {
    return this.page.getByTestId('layers-panel');
  }

  get basemapsPanel() {
    return this.page.getByTestId('basemaps-panel');
  }

  get manageCatalogButton() {
    return this.page.getByRole('button', { name: /manage catalog/i });
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /^save$/i });
  }
}
