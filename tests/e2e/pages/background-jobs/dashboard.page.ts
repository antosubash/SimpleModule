import type { Page } from '@playwright/test';

export class BackgroundJobsDashboardPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/jobs');
  }

  get heading() {
    return this.page.getByRole('heading', { name: 'Background Jobs', exact: true });
  }

  get activeJobsCard() {
    return this.page.getByText(/active jobs/i);
  }

  get failedJobsCard() {
    return this.page.getByText(/failed jobs/i);
  }

  get recurringJobsCard() {
    return this.page.getByText(/recurring jobs/i);
  }

  get viewAllActiveLink() {
    return this.page.getByRole('link', { name: /view all/i }).first();
  }

  get manageRecurringLink() {
    return this.page.getByRole('link', { name: /manage/i });
  }
}
