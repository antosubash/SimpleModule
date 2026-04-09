import type { Page } from '@playwright/test';

export class MapBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/map');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /^maps$/i });
  }

  get manageLayerSourcesButton() {
    return this.page.getByRole('button', { name: /manage layer sources/i });
  }

  get mapCards() {
    return this.page.locator('[data-testid="map-card"]');
  }

  mapCardByName(name: string) {
    return this.page.locator('[data-testid="map-card"]').filter({ hasText: name });
  }
}
