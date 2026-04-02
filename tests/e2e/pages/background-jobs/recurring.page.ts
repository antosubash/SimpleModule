import type { Page } from '@playwright/test';

export class BackgroundJobsRecurringPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/jobs/recurring');
  }

  get heading() {
    return this.page.getByRole('heading', { name: 'Recurring Jobs', exact: true });
  }

  get tableRows() {
    return this.page.locator('table tbody tr');
  }

  jobRow(name: string) {
    return this.page.getByRole('row').filter({ hasText: name });
  }

  toggleButton(name: string) {
    return this.jobRow(name).getByRole('button', { name: /enable|disable/i });
  }

  deleteButton(name: string) {
    return this.jobRow(name).getByRole('button', { name: /delete/i });
  }

  get confirmDeleteButton() {
    return this.page
      .getByRole('alertdialog')
      .or(this.page.getByRole('dialog'))
      .getByRole('button', { name: /confirm|delete|yes/i });
  }
}
