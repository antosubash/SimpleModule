import type { Page } from '@playwright/test';

export class TenantsFeaturesPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /features/i });
  }

  flagRow(flagName: string) {
    return this.page.getByRole('row').filter({ hasText: flagName });
  }

  overrideButton(flagName: string) {
    return this.flagRow(flagName).getByRole('button').first();
  }

  resetButton(flagName: string) {
    return this.flagRow(flagName).getByRole('button', { name: /reset/i });
  }
}
