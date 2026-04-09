import type { Page } from '@playwright/test';

export class DatasetsBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/datasets');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /^datasets$/i });
  }

  get uploadButton() {
    return this.page.getByRole('button', { name: /^upload$/i });
  }

  datasetRowByName(name: string) {
    return this.page.getByRole('row', { name: new RegExp(name, 'i') });
  }
}
