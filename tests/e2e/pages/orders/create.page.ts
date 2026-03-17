import type { Page } from '@playwright/test';

export class OrdersCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/orders/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create order/i });
  }

  get userIdInput() {
    return this.page.locator('#userId');
  }

  get productSelect() {
    return this.page.locator('select').first();
  }

  get quantityInput() {
    return this.page.getByRole('spinbutton').first();
  }

  get addItemButton() {
    return this.page.getByRole('button', { name: /add item/i });
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /create order/i });
  }

  get estimatedTotal() {
    return this.page.getByText(/estimated total/i).locator('..');
  }

  async createOrder(userId: string, productIndex?: number, quantity?: string) {
    await this.userIdInput.fill(userId);
    if (productIndex !== undefined) {
      const options = await this.productSelect.locator('option').all();
      if (options.length > productIndex) {
        const value = await options[productIndex].getAttribute('value');
        if (value) {
          await this.productSelect.selectOption(value);
        }
      }
    }
    if (quantity) {
      await this.quantityInput.fill(quantity);
    }
    // Wait for the POST response before continuing
    await Promise.all([
      this.page.waitForResponse((resp) => resp.request().method() === 'POST'),
      this.submitButton.click(),
    ]);
    await this.page.waitForLoadState('networkidle');
  }
}
