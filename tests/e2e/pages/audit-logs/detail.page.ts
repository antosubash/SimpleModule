import type { Page } from '@playwright/test';

export class AuditLogsDetailPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /audit/i });
  }

  get backButton() {
    return this.page
      .getByRole('link', { name: /back/i })
      .or(this.page.getByRole('button', { name: /back/i }));
  }

  get correlationIdCopyButton() {
    return this.page.getByRole('button', { name: /copy/i });
  }

  get httpDetailsSection() {
    return this.page.getByText(/http details/i);
  }

  get domainDetailsSection() {
    return this.page.getByText(/domain details/i);
  }
}
