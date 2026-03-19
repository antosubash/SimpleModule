import type { Page } from '@playwright/test';

export class UserSettingsPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/settings/me');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /my settings/i });
  }

  get settingCards() {
    return this.page.locator('[data-slot="card"]');
  }

  getResetButton(settingName: string) {
    return this.page
      .locator('div')
      .filter({ hasText: settingName })
      .getByRole('button', { name: /reset/i });
  }

  getBadge(type: 'overridden' | 'default') {
    return this.page.getByText(type === 'overridden' ? 'Overridden' : 'Default');
  }
}
