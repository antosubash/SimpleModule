import { expect, test } from '../../fixtures/base';
import { ProductsBrowsePage } from '../../pages/products/browse.page';
import { ProductsCreatePage } from '../../pages/products/create.page';
import { ProductsEditPage } from '../../pages/products/edit.page';
import { ProductsManagePage } from '../../pages/products/manage.page';

test.describe('Products CRUD', () => {
  test('create, verify, edit, and delete a product', async ({ page, request }) => {
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

    // Find the product ID via API so we can navigate directly to edit
    const productsRes = await request.get('/api/products');
    const products = await productsRes.json();
    const product = products.find((p: { name: string }) => p.name === productName);
    expect(product).toBeTruthy();

    // Edit the product directly by navigating to its edit page
    await page.goto(`/products/${product.id}/edit`);
    await expect(editPage.heading).toBeVisible();
    await editPage.updateProduct(updatedName, '59.99');

    // Verify the update on browse page
    await browsePage.goto();
    await expect(browsePage.productByName(updatedName)).toBeVisible();

    // Delete via API (cleaner than navigating through paginated manage page)
    await request.delete(`/products/${product.id}`);

    // Verify it's gone from browse
    await browsePage.goto();
    await expect(browsePage.productByName(updatedName)).not.toBeVisible();
  });
});
