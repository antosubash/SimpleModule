import type { Page } from '@playwright/test';

export class AdminRolesCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/roles/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create role/i });
  }

  get nameInput() {
    return this.page.getByLabel(/name/i);
  }

  get descriptionInput() {
    return this.page.getByLabel(/description/i);
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /create role/i });
  }

  permissionCheckbox(permission: string) {
    return this.page.getByLabel(permission);
  }

  async createRole(name: string, description?: string) {
    await this.nameInput.fill(name);
    if (description) {
      await this.descriptionInput.fill(description);
    }
    await this.submitButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
