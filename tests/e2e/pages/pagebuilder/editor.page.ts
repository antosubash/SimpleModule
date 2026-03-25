import type { Page } from '@playwright/test';

export class PageBuilderEditorPage {
  constructor(private page: Page) {}

  async gotoNew() {
    await this.page.goto('/admin/pages/new');
    // Wait for either the template picker dialog or the editor to appear
    await Promise.race([
      this.page.getByRole('dialog').waitFor({ state: 'visible', timeout: 10000 }),
      this.page.getByTestId('puck-editor').waitFor({ state: 'visible', timeout: 10000 }),
    ]).catch(() => {});
    // If template picker appeared, dismiss it by clicking Blank Page
    const dialog = this.page.getByRole('dialog');
    if (await dialog.isVisible().catch(() => false)) {
      await dialog.getByRole('button', { name: /blank page/i }).click();
      await this.page.getByTestId('puck-editor').waitFor({ state: 'visible', timeout: 10000 });
    }
  }

  async gotoEdit(id: number) {
    await this.page.goto(`/admin/pages/${id}/edit`);
  }

  get editorOverlay() {
    return this.page.getByTestId('puck-editor');
  }

  get backButton() {
    return this.page.getByRole('button', { name: /back to pages/i });
  }

  get publishButton() {
    return this.page.getByRole('button', { name: /publish/i });
  }

  get puckFrame() {
    return this.page.getByTestId('puck-editor');
  }

  get componentList() {
    return this.page.locator('[class*="ComponentList"]');
  }
}
