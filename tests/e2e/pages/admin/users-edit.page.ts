import type { Page } from '@playwright/test';

export class AdminUsersEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit user/i });
  }

  // Tabs
  get detailsTab() {
    return this.page.getByRole('tab', { name: /details/i });
  }

  get rolesTab() {
    return this.page.getByRole('tab', { name: /roles/i });
  }

  get securityTab() {
    return this.page.getByRole('tab', { name: /security/i });
  }

  get sessionsTab() {
    return this.page.getByRole('tab', { name: /sessions/i });
  }

  // Details tab
  get displayNameInput() {
    return this.page.getByLabel(/display name/i);
  }

  get emailInput() {
    return this.page.getByLabel(/email/i);
  }

  get saveDetailsButton() {
    return this.page.getByRole('button', { name: /save/i });
  }

  get deactivateButton() {
    return this.page.getByRole('button', { name: /deactivate/i });
  }

  get reactivateButton() {
    return this.page.getByRole('button', { name: /reactivate/i });
  }

  // Security tab
  get lockButton() {
    return this.page.getByRole('button', { name: /^lock$/i });
  }

  get unlockButton() {
    return this.page.getByRole('button', { name: /unlock/i });
  }

  get newPasswordInput() {
    return this.page
      .getByLabel('New Password', { exact: true })
      .or(this.page.getByLabel(/new password/i));
  }

  get confirmNewPasswordInput() {
    return this.page.getByLabel(/confirm/i);
  }

  get resetPasswordButton() {
    return this.page.getByRole('button', { name: /reset password/i });
  }

  // Roles tab
  roleCheckbox(roleName: string) {
    return this.page.getByLabel(roleName);
  }

  get saveRolesButton() {
    return this.page.getByRole('button', { name: /save/i });
  }

  // Dialog confirmation
  get confirmButton() {
    return this.page
      .getByRole('alertdialog')
      .or(this.page.getByRole('dialog'))
      .getByRole('button', { name: /confirm|yes|continue/i });
  }

  async updateDisplayName(name: string) {
    await this.detailsTab.click();
    await this.displayNameInput.fill(name);
    await this.saveDetailsButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
