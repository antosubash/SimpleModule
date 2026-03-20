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
    return this.page.getByRole('button', { name: /add item/i });
  }

  get addChildButton() {
    return this.page.getByRole('button', { name: /add child/i });
  }

  get saveButton() {
    return this.page.getByRole('button', { name: /save/i });
  }

  get deleteButton() {
    return this.page.getByRole('button', { name: /delete/i });
  }

  get labelInput() {
    return this.page.locator('input#label');
  }

  get urlInput() {
    return this.page.locator('input#url');
  }

  get pageSelect() {
    return this.page.locator('select#pageRoute');
  }

  get pageRadio() {
    return this.page.locator('input[type="radio"][value="page"]');
  }

  get urlRadio() {
    return this.page.locator('input[type="radio"][value="url"]');
  }

  get openInNewTabSwitch() {
    return this.page.locator('#openInNewTab');
  }

  get visibleSwitch() {
    return this.page.locator('#isVisible');
  }

  get homePageSwitch() {
    return this.page.locator('#isHomePage');
  }

  get emptyState() {
    return this.page.getByText(/no menu items yet/i);
  }

  treeItemButton(label: string) {
    return this.page.getByRole('button', { name: new RegExp(label, 'i') });
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
