import type { Page } from '@playwright/test';

export class DatasetsDetailPage {
  constructor(
    private page: Page,
    private datasetId: string,
  ) {}

  async goto() {
    await this.page.goto(`/datasets/${this.datasetId}`);
  }

  get heading() {
    return this.page.locator('h1').first();
  }

  get downloadOriginalButton() {
    return this.page.getByRole('button', { name: /download original/i });
  }

  get deleteButton() {
    return this.page.getByRole('button', { name: /^delete$/i });
  }
}
