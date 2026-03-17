import type { Page } from '@playwright/test';

export class OrdersEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit order/i });
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

  get saveButton() {
    return this.page.getByRole('button', { name: /save changes/i });
  }

  get deleteButton() {
    return this.page.getByRole('button', { name: /delete order/i });
  }

  async updateOrder(userId: string, quantity?: string) {
    await this.userIdInput.fill(userId);
    if (quantity) {
      await this.quantityInput.fill(quantity);
    }
    await this.saveButton.click();
    // Wait for Inertia to complete the update round-trip
    await this.page.waitForLoadState('networkidle');
  }
}
