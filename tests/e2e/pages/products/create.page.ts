import type { Page } from '@playwright/test';

export class ProductsCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create/i });
  }

  get nameInput() {
    return this.page.getByLabel('Name');
  }

  get priceInput() {
    return this.page.getByLabel('Price');
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /save|create|submit/i });
  }

  async createProduct(name: string, price: string) {
    await this.nameInput.fill(name);
    await this.priceInput.fill(price);
    // Wait for the POST response before continuing
    await Promise.all([
      this.page.waitForResponse((resp) => resp.request().method() === 'POST'),
      this.submitButton.click(),
    ]);
    await this.page.waitForLoadState('networkidle');
  }
}
