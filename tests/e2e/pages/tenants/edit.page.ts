import type { Page } from '@playwright/test';

export class TenantsEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit tenant/i });
  }

  get nameInput() {
    return this.page.getByLabel(/name/i);
  }

  get adminEmailInput() {
    return this.page.getByLabel(/admin email/i);
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /save/i });
  }

  get manageFeaturesButton() {
    return this.page
      .getByRole('link', { name: /manage features/i })
      .or(this.page.getByRole('button', { name: /manage features/i }));
  }

  get addHostInput() {
    return this.page.getByPlaceholder(/host/i);
  }

  get addHostButton() {
    return this.page.getByRole('button', { name: /add host/i });
  }

  hostRow(hostName: string) {
    return this.page.getByRole('row').filter({ hasText: hostName });
  }

  removeHostButton(hostName: string) {
    return this.hostRow(hostName).getByRole('button', { name: /remove/i });
  }

  async updateName(name: string) {
    await this.nameInput.fill(name);
    await this.saveButton.click();
    await this.page.waitForLoadState('networkidle');
  }

  async addHost(hostName: string) {
    await this.addHostInput.fill(hostName);
    await this.addHostButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
