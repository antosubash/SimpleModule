import type { Page } from '@playwright/test';

export class AdminSettingsPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/settings');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /settings/i });
  }

  get systemTab() {
    return this.page.getByRole('tab', { name: /system/i });
  }

  get applicationTab() {
    return this.page.getByRole('tab', { name: /application/i });
  }

  get settingCards() {
    return this.page.getByTestId('setting-card');
  }
}
