import type { Page } from '@playwright/test';

export class OrdersListPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/orders');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /orders/i });
  }

  get createOrderButton() {
    return this.page.getByRole('button', { name: /create order/i });
  }

  get emptyMessage() {
    return this.page.getByText(/no orders yet/i);
  }

  orderRowByUser(userId: string) {
    return this.page.getByRole('row', { name: new RegExp(userId, 'i') });
  }

  editButton(userId: string) {
    return this.orderRowByUser(userId).getByRole('button', { name: /edit/i });
  }

  deleteButton(userId: string) {
    return this.orderRowByUser(userId).getByRole('button', { name: /delete/i });
  }
}
