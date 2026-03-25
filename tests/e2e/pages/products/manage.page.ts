import type { Page } from '@playwright/test';

export class ProductsManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/manage');
  }

  async showAllRows() {
    // Change the DataGrid page size to 50 to show all items
    await this.page.getByTestId('datagrid-page-size').click();
    await this.page.getByRole('option', { name: '50' }).click();
  }

  get heading() {
    return this.page.getByRole('heading', { name: /manage|products/i });
  }

  editButton(productName: string) {
    return this.page
      .getByRole('row', { name: new RegExp(productName, 'i') })
      .getByRole('button', { name: /edit/i });
  }

  deleteButton(productName: string) {
    return this.page
      .getByRole('row', { name: new RegExp(productName, 'i') })
      .getByRole('button', { name: /delete/i });
  }

  productRow(name: string) {
    return this.page.getByRole('row', { name: new RegExp(name, 'i') });
  }
}
