import { expect, test } from '../../fixtures/base';
import { DatasetsBrowsePage } from '../../pages/datasets/browse.page';
import { DatasetsUploadPage } from '../../pages/datasets/upload.page';

test.describe('Datasets pages', () => {
  test('browse page loads', async ({ page }) => {
    const browse = new DatasetsBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });

  test('browse page shows upload button', async ({ page }) => {
    const browse = new DatasetsBrowsePage(page);
    await browse.goto();
    await expect(browse.uploadButton).toBeVisible();
  });

  test('upload page loads', async ({ page }) => {
    const upload = new DatasetsUploadPage(page);
    await upload.goto();
    await expect(upload.heading).toBeVisible();
    await expect(upload.chooseFileButton).toBeVisible();
  });
});
