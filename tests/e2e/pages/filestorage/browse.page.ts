import type { Page } from '@playwright/test';

export class FileStorageBrowsePage {
  constructor(private readonly page: Page) {}

  get heading() {
    return this.page.getByRole('heading', { level: 1 });
  }

  get uploadButton() {
    return this.page.getByRole('button', { name: /upload file/i });
  }

  get emptyTitle() {
    return this.page.getByRole('heading', { name: /no files yet/i });
  }

  get emptyDescription() {
    return this.page.getByText(/upload a file to get started/i);
  }

  get description() {
    return this.page.locator('p').filter({ hasText: /\d+ file/ });
  }

  fileRow(name: string) {
    return this.page.getByRole('row', { name: new RegExp(name, 'i') });
  }

  folderRow(name: string) {
    return this.page.getByRole('row', { name: new RegExp(`📁.*${name}`, 'i') });
  }

  downloadButton(fileName: string) {
    return this.fileRow(fileName).getByRole('button', { name: /download/i });
  }

  deleteButton(fileName: string) {
    return this.fileRow(fileName).getByRole('button', { name: /delete/i });
  }

  breadcrumb(label: string) {
    return this.page.getByRole('button', { name: label });
  }

  get parentRow() {
    return this.page.getByRole('row', { name: '..' });
  }

  get deleteDialogTitle() {
    return this.page.getByRole('heading', { name: /delete file/i });
  }

  get deleteConfirmButton() {
    return this.page.getByRole('dialog').getByRole('button', { name: /^delete$/i });
  }

  get deleteCancelButton() {
    return this.page.getByRole('dialog').getByRole('button', { name: /cancel/i });
  }
}
