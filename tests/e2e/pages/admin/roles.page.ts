import type { Page } from '@playwright/test';

export class AdminRolesPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/roles');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /roles/i });
  }

  get createButton() {
    return this.page
      .getByRole('link', { name: /create role/i })
      .or(this.page.getByRole('button', { name: /create role/i }));
  }

  roleRow(name: string) {
    return this.page.getByRole('row').filter({ hasText: name });
  }

  editButton(name: string) {
    return this.roleRow(name)
      .getByRole('link', { name: /edit/i })
      .or(this.roleRow(name).getByRole('button', { name: /edit/i }));
  }

  deleteButton(name: string) {
    return this.roleRow(name).getByRole('button', { name: /delete/i });
  }

  get confirmDeleteButton() {
    return this.page
      .getByRole('alertdialog')
      .or(this.page.getByRole('dialog'))
      .getByRole('button', { name: /confirm|delete|yes/i });
  }
}
