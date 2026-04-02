import type { Page } from '@playwright/test';

export class AdminRolesEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit role/i });
  }

  // Tabs
  get detailsTab() {
    return this.page.getByRole('tab', { name: /details/i });
  }

  get permissionsTab() {
    return this.page.getByRole('tab', { name: /permissions/i });
  }

  get usersTab() {
    return this.page.getByRole('tab', { name: /users/i });
  }

  // Details tab
  get nameInput() {
    return this.page.getByLabel(/name/i);
  }

  get descriptionInput() {
    return this.page.getByLabel(/description/i);
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /save/i });
  }

  // Permissions tab
  permissionCheckbox(permission: string) {
    return this.page.getByLabel(permission);
  }

  get savePermissionsButton() {
    return this.page.getByRole('button', { name: /save permissions/i });
  }

  async updateName(name: string) {
    await this.detailsTab.click();
    await this.nameInput.fill(name);
    await this.saveButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
