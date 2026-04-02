import type { Page } from '@playwright/test';

export class BackgroundJobsDetailPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /job detail/i });
  }

  get cancelButton() {
    return this.page.getByRole('button', { name: /cancel/i });
  }

  get retryButton() {
    return this.page.getByRole('button', { name: /retry/i });
  }

  get backToListButton() {
    return this.page
      .getByRole('link', { name: /back/i })
      .or(this.page.getByRole('button', { name: /back/i }));
  }

  get logsSection() {
    return this.page.getByText(/logs/i);
  }

  get statusCard() {
    return this.page.getByText(/status/i);
  }
}
