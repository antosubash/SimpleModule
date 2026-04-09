import type { Page } from '@playwright/test';

export class MapEditPage {
  constructor(
    private page: Page,
    private mapId: string,
  ) {}

  async goto() {
    await this.page.goto(`/map/${this.mapId}/edit`);
  }

  get heading() {
    // Edit page uses the saved map name as the h1 or name input — fall back to the name input label
    return this.page.getByLabel('Name');
  }
}
