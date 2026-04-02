import type { Page } from '@playwright/test';

export class TenantsCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/tenants/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create tenant/i });
  }

  get nameInput() {
    return this.page.getByLabel(/name/i);
  }

  get slugInput() {
    return this.page.getByLabel(/slug/i);
  }

  get adminEmailInput() {
    return this.page.getByLabel(/admin email/i);
  }

  get editionInput() {
    return this.page.getByLabel(/edition/i);
  }

  get submitButton() {
    return this.page.getByRole('button', { name: /create/i });
  }

  async createTenant(name: string, slug: string, adminEmail?: string) {
    await this.nameInput.fill(name);
    await this.slugInput.fill(slug);
    if (adminEmail) {
      await this.adminEmailInput.fill(adminEmail);
    }
    await this.submitButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
