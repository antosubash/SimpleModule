import type { Page } from '@playwright/test';

export class DashboardPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/');
  }

  get heading() {
    return this.page.getByRole('heading').first();
  }
}
