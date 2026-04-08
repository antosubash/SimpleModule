import type { Page } from '@playwright/test';

export class FeatureFlagsManagePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/feature-flags/manage');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /feature flags/i });
  }

  get flagTable() {
    return this.page.getByRole('table');
  }

  get flagRows() {
    return this.page.getByRole('row');
  }

  flagRow(name: string) {
    return this.page.getByRole('row', { name: new RegExp(name, 'i') });
  }

  flagToggle(name: string) {
    return this.flagRow(name).getByRole('switch');
  }

  overridesButton(name: string) {
    return this.flagRow(name).getByRole('button', { name: /overrides/i });
  }

  get overrideDialog() {
    return this.page.getByRole('dialog');
  }

  get addOverrideButton() {
    return this.overrideDialog.getByRole('button', { name: /add override/i });
  }

  get overrideValueInput() {
    return this.overrideDialog.getByLabel(/value/i);
  }

  get deprecatedSection() {
    return this.page.getByText(/deprecated/i);
  }
}
