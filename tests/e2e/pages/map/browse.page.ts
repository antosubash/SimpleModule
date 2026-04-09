import type { Page } from '@playwright/test';

export class MapBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/map');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /default map/i });
  }

  get manageCatalogButton() {
    return this.page.getByRole('button', { name: /manage catalog/i });
  }

  get sidePanel() {
    return this.page.locator('[data-testid="map-side-panel"]');
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /^save$/i });
  }
}
