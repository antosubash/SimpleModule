import type { Page } from '@playwright/test';

export class PageBuilderManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/pages');
  }

  async showAllRows() {
    await this.page.getByTestId('datagrid-page-size').click();
    await this.page.getByRole('option', { name: '50' }).click();
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
    return this.page.locator('table tbody').getByRole('row');
  }

  pageRow(title: string) {
    return this.page.getByRole('row', { name: new RegExp(title, 'i') });
  }

  /** Opens the Actions dropdown for a row and clicks Edit */
  async clickEdit(title: string) {
    await this.pageRow(title)
      .getByRole('button', { name: new RegExp(`actions for ${title}`, 'i') })
      .click();
    await this.page.getByRole('menuitem', { name: /edit/i }).click();
  }

  /** Opens the Actions dropdown for a row and clicks Delete */
  async clickDelete(title: string) {
    await this.pageRow(title)
      .getByRole('button', { name: new RegExp(`actions for ${title}`, 'i') })
      .click();
    await this.page.getByRole('menuitem', { name: /delete/i }).click();
  }

  // Keep old API for backwards compatibility but point to dropdown actions
  editButton(title: string) {
    return this.pageRow(title).getByRole('button', {
      name: new RegExp(`actions for ${title}`, 'i'),
    });
  }

  deleteButton(title: string) {
    return this.pageRow(title).getByRole('button', {
      name: new RegExp(`actions for ${title}`, 'i'),
    });
  }

  statusBadge(title: string) {
    return this.pageRow(title).getByTestId('status-badge');
  }
}
