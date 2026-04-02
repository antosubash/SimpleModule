import type { Page } from '@playwright/test';

export class EnableAuthenticatorPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/Identity/Account/Manage/EnableAuthenticator');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /authenticator/i });
  }

  get sharedKey() {
    return this.page.locator('code, kbd, [class*="key"]');
  }

  get qrCode() {
    return this.page.locator('img[src*="qr"], img[alt*="qr"], canvas, svg');
  }

  get codeInput() {
    return this.page.getByLabel(/code/i).or(this.page.locator('input[name="code"]'));
  }

  get verifyButton() {
    return this.page.getByRole('button', { name: /verify/i });
  }
}
