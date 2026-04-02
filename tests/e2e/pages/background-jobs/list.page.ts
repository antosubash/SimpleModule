import type { Page } from '@playwright/test';

export class BackgroundJobsListPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/jobs/list');
  }

  get heading() {
    return this.page.getByRole('heading', { name: 'All Jobs', exact: true });
  }

  get tableRows() {
    return this.page.locator('table tbody tr');
  }

  jobRow(jobType: string) {
    return this.page.getByRole('row').filter({ hasText: jobType });
  }
}
