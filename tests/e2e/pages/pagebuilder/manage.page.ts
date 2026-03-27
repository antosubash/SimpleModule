import type { Page } from '@playwright/test';

export class PageBuilderManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/pages/manage');
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

  /** Find a row by slug (more reliable than title for auto-generated pages) */
  pageRowBySlug(slug: string) {
    return this.page.getByRole('row').filter({ hasText: slug });
  }

  /** Open actions dropdown for a row found by slug and click a menu item */
  async clickActionBySlug(slug: string, action: RegExp) {
    const row = this.pageRowBySlug(slug);
    await row.getByRole('button', { name: /actions/i }).click();
    await this.page.getByRole('menuitem', { name: action }).click();
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

  /** Opens the Actions dropdown for a row and clicks Publish or Unpublish */
  async clickPublishToggle(title: string) {
    await this.pageRow(title)
      .getByRole('button', { name: new RegExp(`actions for ${title}`, 'i') })
      .click();
    // Click whichever is visible — "Publish" or "Unpublish"
    const publish = this.page.getByRole('menuitem', { name: /publish/i });
    await publish.click();
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
