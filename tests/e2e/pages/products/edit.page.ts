import type { Page } from '@playwright/test';

export class ProductsEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit/i });
  }

  get nameInput() {
    return this.page.getByLabel('Name');
  }

  get priceInput() {
    return this.page.getByLabel('Price');
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /save|update|submit/i });
  }

  async updateProduct(name: string, price: string) {
    await this.nameInput.clear();
    await this.nameInput.fill(name);
    await this.priceInput.clear();
    await this.priceInput.fill(price);
    // Wait for the POST response before continuing
    await Promise.all([
      this.page.waitForResponse((resp) => resp.request().method() === 'POST'),
      this.submitButton.click(),
    ]);
    await this.page.waitForLoadState('networkidle');
  }
}
