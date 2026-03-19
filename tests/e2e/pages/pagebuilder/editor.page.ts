import type { Page } from '@playwright/test';

export class PageBuilderEditorPage {
  constructor(private page: Page) {}

  async gotoNew() {
    await this.page.goto('/admin/pages/new');
  }

  async gotoEdit(id: number) {
    await this.page.goto(`/admin/pages/${id}/edit`);
  }

  get editorOverlay() {
    return this.page.locator('[style*="position: fixed"][style*="z-index"]');
  }

  get backButton() {
    return this.page.getByRole('button', { name: /back/i });
  }

  get publishButton() {
    return this.page.getByRole('button', { name: /publish/i });
  }

  get puckFrame() {
    return this.page.locator('[class*="Puck"]');
  }

  get componentList() {
    return this.page.locator('[class*="ComponentList"]');
  }
}
