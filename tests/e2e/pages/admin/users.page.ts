import type { Page } from '@playwright/test';

export class AdminUsersPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/admin/users');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /users/i });
  }

  get createButton() {
    return this.page
      .getByRole('link', { name: /create user/i })
      .or(this.page.getByRole('button', { name: /create user/i }));
  }

  get searchInput() {
    return this.page.getByPlaceholder(/search/i);
  }

  get searchButton() {
    return this.page.getByRole('button', { name: /search/i });
  }

  userRow(email: string) {
    return this.page.getByRole('row').filter({ hasText: email });
  }

  editButton(email: string) {
    return this.userRow(email)
      .getByRole('link', { name: /edit/i })
      .or(this.userRow(email).getByRole('button', { name: /edit/i }));
  }

  async search(term: string) {
    await this.searchInput.fill(term);
    await this.searchButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
