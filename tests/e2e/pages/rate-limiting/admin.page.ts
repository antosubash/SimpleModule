import type { Page } from '@playwright/test';

export class RateLimitingAdminPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/rate-limiting');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /rate limiting/i });
  }

  get storedRulesTab() {
    return this.page.getByRole('tab', { name: /stored rules/i });
  }

  get activePoliciesTab() {
    return this.page.getByRole('tab', { name: /active policies/i });
  }

  get createRuleButton() {
    return this.page.getByRole('button', { name: /create rule/i });
  }

  get createDialog() {
    return this.page.getByRole('dialog');
  }

  get policyNameInput() {
    return this.createDialog.getByLabel(/policy name/i);
  }

  get permitLimitInput() {
    return this.createDialog.getByLabel(/permit limit/i);
  }

  get createSubmitButton() {
    return this.createDialog.getByRole('button', { name: /^create$/i });
  }

  get rulesTable() {
    return this.page.getByRole('table');
  }

  get activePoliciesTable() {
    return this.page.getByRole('table');
  }

  ruleRow(name: string) {
    return this.page.getByRole('row', { name: new RegExp(name, 'i') });
  }

  ruleToggle(name: string) {
    return this.ruleRow(name).getByRole('switch');
  }

  ruleDeleteButton(name: string) {
    return this.ruleRow(name).getByRole('button', { name: /delete/i });
  }
}
