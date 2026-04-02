import type { Page } from '@playwright/test';

export class AuditLogsDashboardPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/audit-logs/dashboard');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /audit/i });
  }

  get last24hButton() {
    return this.page.getByRole('button', { name: /last 24h/i });
  }

  get last7daysButton() {
    return this.page.getByRole('button', { name: /last 7 days/i });
  }

  get last30daysButton() {
    return this.page.getByRole('button', { name: /last 30 days/i });
  }

  get applyButton() {
    return this.page.getByRole('button', { name: /apply/i });
  }

  get totalEventsCard() {
    return this.page.getByText(/total events/i);
  }
}
