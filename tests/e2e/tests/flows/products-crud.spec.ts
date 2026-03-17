import { expect, test } from '../../fixtures/base';
import { ProductsBrowsePage } from '../../pages/products/browse.page';
import { ProductsCreatePage } from '../../pages/products/create.page';
import { ProductsEditPage } from '../../pages/products/edit.page';
import { ProductsManagePage } from '../../pages/products/manage.page';

test.describe('Products CRUD', () => {
  test('create, verify, edit, and delete a product', async ({ page }) => {
    const suffix = Date.now();
    const productName = `E2E Product ${suffix}`;
    const updatedName = `E2E Updated ${suffix}`;

    const createPage = new ProductsCreatePage(page);
    const browsePage = new ProductsBrowsePage(page);
    const managePage = new ProductsManagePage(page);
    const editPage = new ProductsEditPage(page);

    // Create a product
    await createPage.goto();
    await createPage.createProduct(productName, '49.99');

    // Verify it appears on browse page
    await browsePage.goto();
    await expect(browsePage.productByName(productName)).toBeVisible();

    // Edit the product via manage page
    await managePage.goto();
    await managePage.editButton(productName).click();
    await expect(editPage.heading).toBeVisible();
    await editPage.updateProduct(updatedName, '59.99');

    // Verify the update on browse page
    await browsePage.goto();
    await expect(browsePage.productByName(updatedName)).toBeVisible();

    // Delete the product via manage page (confirm dialog will appear)
    await managePage.goto();
    page.on('dialog', (dialog) => dialog.accept());
    await managePage.deleteButton(updatedName).click();
    await page.waitForLoadState('networkidle');

    // Verify it's gone
    await expect(managePage.productRow(updatedName)).not.toBeVisible();
  });
});
