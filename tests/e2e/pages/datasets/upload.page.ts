import type { Page } from '@playwright/test';

export class DatasetsUploadPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/datasets/upload');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /upload gis dataset/i });
  }

  get chooseFileButton() {
    return this.page.getByRole('button', { name: /choose file/i });
  }

  get hiddenFileInput() {
    return this.page.locator('input[type="file"]');
  }

  get status() {
    return this.page.locator('text=/uploading|processing|upload failed|timed out/i');
  }
}
