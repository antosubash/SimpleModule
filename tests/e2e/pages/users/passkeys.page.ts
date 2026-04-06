import type { Page } from '@playwright/test';

export class PasskeysPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/Identity/Account/Manage/Passkeys');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /passkeys/i });
  }

  get addPasskeyButton() {
    return this.page.getByRole('button', { name: /add passkey/i });
  }

  get emptyState() {
    return this.page.getByText(/no passkeys registered yet/i);
  }

  get removeButtons() {
    return this.page.getByRole('button', { name: /remove/i });
  }
}
