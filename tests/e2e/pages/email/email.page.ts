import type { Page } from '@playwright/test';

export class EmailDashboardPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/email/dashboard');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /email dashboard/i });
  }
}

export class EmailTemplatesPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/email/templates');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /email templates/i });
  }

  get newTemplateButton() {
    return this.page.getByRole('button', { name: /new template/i });
  }
}

export class EmailCreateTemplatePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/email/templates/create');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /create template/i });
  }
}

export class EmailHistoryPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/email/history');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /email history/i });
  }
}

export class EmailSettingsPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/email/settings');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /email settings/i });
  }
}
