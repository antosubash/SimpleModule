import type { Page } from '@playwright/test';

export class MenuManagerPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/settings/menus');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /menu manager/i });
  }

  get addItemButton() {
    return this.page.getByRole('button', { name: /^add$/i });
  }

  get addChildButton() {
    return this.page.getByRole('button', { name: /^child$/i });
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /save/i });
  }

  get deleteButton() {
    return this.page.getByRole('button', { name: 'Delete', exact: true });
  }

  get settingsTab() {
    return this.page.getByRole('tab', { name: /settings/i });
  }

  get labelInput() {
    return this.page.getByLabel('Label');
  }

  get urlInput() {
    return this.page.locator('#url');
  }

  get pageSelect() {
    return this.page.getByRole('combobox');
  }

  get pageRadio() {
    return this.page.getByRole('radio', { name: /page/i });
  }

  get urlRadio() {
    return this.page.getByRole('radio', { name: /url/i });
  }

  get openInNewTabSwitch() {
    return this.page.getByLabel('Open in New Tab');
  }

  get visibleSwitch() {
    return this.page.getByLabel('Visible');
  }

  get homePageSwitch() {
    return this.page.getByLabel('Home Page');
  }

  get emptyState() {
    return this.page.getByText(/no menu items yet/i);
  }

  /** Scoped to tree area — use exact label to avoid duplicates */
  treeItemButton(label: string) {
    return this.page.getByRole('button', { name: new RegExp(`^${label}\\b`, 'i') }).first();
  }

  async selectItem(label: string) {
    await this.treeItemButton(label).click();
  }

  visibilityToggle(label: string) {
    return this.treeItemButton(label).getByRole('switch');
  }

  homeBadge(label: string) {
    return this.treeItemButton(label).getByText('Home');
  }
}
