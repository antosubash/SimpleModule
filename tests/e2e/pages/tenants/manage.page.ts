import type { Page } from '@playwright/test';

export class TenantsManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/tenants/manage');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /manage tenants/i });
  }

  get createButton() {
    return this.page.getByRole('button', { name: /create tenant/i });
  }

  tenantRow(name: string) {
    return this.page.getByRole('row').filter({ hasText: name });
  }

  editButton(name: string) {
    return this.tenantRow(name).getByRole('button', { name: /edit/i });
  }

  featuresButton(name: string) {
    return this.tenantRow(name).getByRole('button', { name: /features/i });
  }

  deleteButton(name: string) {
    return this.tenantRow(name).getByRole('button', { name: /delete/i });
  }

  get confirmDeleteButton() {
    return this.page
      .getByRole('alertdialog')
      .or(this.page.getByRole('dialog'))
      .getByRole('button', { name: /confirm|delete|yes/i });
  }
}
