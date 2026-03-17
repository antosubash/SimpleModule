import type { Page } from '@playwright/test';

export class ProductsManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/manage');
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
