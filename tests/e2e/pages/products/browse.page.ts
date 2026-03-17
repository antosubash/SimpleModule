import type { Page } from '@playwright/test';

export class ProductsBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/products/browse');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /products/i });
  }

  get productCards() {
    return this.page.locator('[data-testid="product-card"]');
  }

  productByName(name: string) {
    return this.page.getByText(name);
  }
}
