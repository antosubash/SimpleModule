import type { Page } from '@playwright/test';

export class TenantsBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/tenants/browse');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /tenants/i });
  }

  tenantCard(name: string) {
    return this.page.getByText(name);
  }
}
