import type { Page } from '@playwright/test';

export class PageBuilderPagesListPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/pages');
  }

  get heading() {
    return this.page.getByRole('heading', { name: 'Pages', exact: true });
  }

  get emptyMessage() {
    return this.page.getByText(/no published pages/i);
  }

  get pageLinks() {
    return this.page.locator('ul a');
  }

  pageLinkByTitle(title: string) {
    return this.page.getByRole('link', { name: new RegExp(title, 'i') });
  }
}
