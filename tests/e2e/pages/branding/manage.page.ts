import type { Page } from '@playwright/test';

export class BrandingManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/branding/manage');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /branding/i, level: 1 });
  }

  get appNameInput() {
    return this.page.getByRole('textbox', { name: 'Application name' });
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /save changes/i });
  }

  get showTopBarSwitch() {
    return this.page.getByRole('switch', { name: /show top bar/i });
  }

  get showFooterSwitch() {
    return this.page.getByRole('switch', { name: /show footer/i });
  }
}
