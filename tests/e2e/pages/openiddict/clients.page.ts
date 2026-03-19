import type { Page } from '@playwright/test';

export class ClientsPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/openiddict/clients');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /clients/i });
  }

  get createButton() {
    return this.page.getByRole('button', { name: 'Create Client' });
  }

  clientRow(clientId: string) {
    return this.page.getByRole('row').filter({ hasText: clientId });
  }

  editButton(clientId: string) {
    return this.clientRow(clientId).getByRole('button', { name: 'Edit' });
  }

  deleteButton(clientId: string) {
    return this.clientRow(clientId).getByRole('button', { name: 'Delete' });
  }
}

export class ClientsCreatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/openiddict/clients/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create client/i });
  }

  async createClient(clientId: string, displayName: string, type: 'public' | 'confidential' = 'public') {
    await this.page.getByLabel('Client ID').fill(clientId);
    await this.page.getByLabel('Display Name').fill(displayName);
    await this.page.getByLabel('Client Type').selectOption(type);
    await this.page.getByRole('button', { name: 'Create Client' }).click();
  }
}

export class ClientsEditPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { name: /edit client/i });
  }

  get detailsTab() {
    return this.page.getByRole('button', { name: 'Details' });
  }

  get urisTab() {
    return this.page.getByRole('button', { name: 'URIs' });
  }

  get permissionsTab() {
    return this.page.getByRole('button', { name: 'Permissions' });
  }

  get displayNameInput() {
    return this.page.getByLabel('Display Name');
  }

  get saveButton() {
    return this.page.getByRole('button', { name: 'Save' });
  }

  async updateDisplayName(name: string) {
    await this.displayNameInput.fill(name);
    await this.saveButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
