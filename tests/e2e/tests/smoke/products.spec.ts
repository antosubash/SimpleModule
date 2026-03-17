import { expect, test } from '../../fixtures/base';
import { ProductsBrowsePage } from '../../pages/products/browse.page';
import { ProductsCreatePage } from '../../pages/products/create.page';
import { ProductsManagePage } from '../../pages/products/manage.page';

test.describe('Products pages', () => {
  test('browse page loads', async ({ page }) => {
    const browse = new ProductsBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });

  test('create page loads', async ({ page }) => {
    const create = new ProductsCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });

  test('manage page loads', async ({ page }) => {
    const manage = new ProductsManagePage(page);
    await manage.goto();
    await expect(manage.heading).toBeVisible();
  });
});
