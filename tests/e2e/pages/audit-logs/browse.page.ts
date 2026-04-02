import type { Page } from '@playwright/test';

export class AuditLogsBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/audit-logs/browse');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /audit/i });
  }

  get searchInput() {
    return this.page.getByPlaceholder(/search/i);
  }

  get applyButton() {
    return this.page.getByRole('button', { name: /apply/i });
  }

  get clearFiltersButton() {
    return this.page.getByRole('button', { name: /clear/i });
  }

  get exportCsvButton() {
    return this.page.getByRole('button', { name: /csv/i });
  }

  get exportJsonButton() {
    return this.page.getByRole('button', { name: /json/i });
  }

  get tableRows() {
    return this.page.locator('table tbody tr');
  }

  get nextPageButton() {
    return this.page.getByRole('button', { name: /next/i });
  }

  get previousPageButton() {
    return this.page.getByRole('button', { name: /previous/i });
  }

  entryRow(index: number) {
    return this.page.locator('table tbody tr').nth(index);
  }
}
