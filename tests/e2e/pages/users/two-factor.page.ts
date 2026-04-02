import type { Page } from '@playwright/test';

export class TwoFactorPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/Identity/Account/Manage/TwoFactorAuthentication');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /two-factor/i });
  }

  get addAuthenticatorButton() {
    return this.page
      .getByRole('link', { name: /add authenticator/i })
      .or(this.page.getByRole('button', { name: /add authenticator/i }));
  }

  get setupAuthenticatorButton() {
    return this.page
      .getByRole('link', { name: /set up authenticator/i })
      .or(this.page.getByRole('button', { name: /set up authenticator/i }));
  }

  get disable2faButton() {
    return this.page
      .getByRole('link', { name: /disable 2fa/i })
      .or(this.page.getByRole('button', { name: /disable/i }));
  }

  get forgetBrowserButton() {
    return this.page.getByRole('button', { name: /forget this browser/i });
  }

  get resetRecoveryCodesButton() {
    return this.page
      .getByRole('link', { name: /reset recovery/i })
      .or(this.page.getByRole('button', { name: /reset recovery/i }));
  }
}
