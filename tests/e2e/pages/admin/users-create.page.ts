import type { Page } from '@playwright/test';

export class AdminUsersCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/users/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create user/i });
  }

  get displayNameInput() {
    return this.page.getByLabel(/display name/i);
  }

  get emailInput() {
    return this.page.getByLabel('Email', { exact: true });
  }

  get passwordInput() {
    return this.page.getByLabel('Password', { exact: true });
  }

  get confirmPasswordInput() {
    return this.page.getByLabel(/confirm password/i);
  }

  get emailConfirmedCheckbox() {
    return this.page.getByLabel(/email confirmed/i);
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /create user/i });
  }

  roleCheckbox(roleName: string) {
    return this.page.getByLabel(roleName);
  }

  async createUser(
    displayName: string,
    email: string,
    password: string,
    options?: { confirmEmail?: boolean; roles?: string[] },
  ) {
    await this.displayNameInput.fill(displayName);
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.confirmPasswordInput.fill(password);
    if (options?.confirmEmail) {
      await this.emailConfirmedCheckbox.check();
    }
    if (options?.roles) {
      for (const role of options.roles) {
        await this.roleCheckbox(role).check();
      }
    }
    await this.submitButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
