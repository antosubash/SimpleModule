import type { Page } from '@playwright/test';

export class PageBuilderManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/pages');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /pages/i });
  }

  get newPageButton() {
    return this.page.getByRole('button', { name: /new page/i });
  }

  get emptyMessage() {
    return this.page.getByText(/no pages yet/i);
  }

  get tableRows() {
    return this.page.locator('table tbody tr');
  }

  pageRow(title: string) {
    return this.page.getByRole('row', { name: new RegExp(title, 'i') });
  }

  editButton(title: string) {
    return this.pageRow(title).getByRole('button', { name: /edit/i });
  }

  deleteButton(title: string) {
    return this.pageRow(title).getByRole('button', { name: /delete/i });
  }

  publishButton(title: string) {
    return this.pageRow(title).getByRole('button', { name: /publish/i });
  }

  unpublishButton(title: string) {
    return this.pageRow(title).getByRole('button', { name: /unpublish/i });
  }

  statusBadge(title: string) {
    return this.pageRow(title).locator('[class*="badge"]');
  }
}
