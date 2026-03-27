import type { Page } from '@playwright/test';

export class PageBuilderViewerPage {
  constructor(private page: Page) {}

  async goto(slug: string) {
    await this.page.goto(`/pages/view/${slug}`);
  }

  get content() {
    return this.page.getByTestId('page-content');
  }
}
