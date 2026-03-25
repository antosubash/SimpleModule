import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { ProductsBrowsePage } from '../../pages/products/browse.page';
import { ProductsCreatePage } from '../../pages/products/create.page';
import { ProductsEditPage } from '../../pages/products/edit.page';

test.describe('Products CRUD', () => {
  test('create, verify, edit, and delete a product', async ({ page, request }) => {
    const productName = faker.commerce.productName();
    const productPrice = faker.commerce.price({ min: 10, max: 500 });
    const updatedName = faker.commerce.productName();
    const updatedPrice = faker.commerce.price({ min: 10, max: 500 });

    const createPage = new ProductsCreatePage(page);
    const browsePage = new ProductsBrowsePage(page);
    const editPage = new ProductsEditPage(page);

    // Create a product via UI
    await createPage.goto();
    await createPage.createProduct(productName, productPrice);

    // UI: verify it appears on browse page
    await browsePage.goto();
    await expect(browsePage.productByName(productName)).toBeVisible();

    // API: verify the product was persisted correctly
    const productsRes = await request.get('/api/products');
    expect(productsRes.ok()).toBeTruthy();
    const products = await productsRes.json();
    const product = products.find((p: { name: string }) => p.name === productName);
    expect(product).toBeTruthy();
    expect(Number(product.price)).toBeCloseTo(Number(productPrice), 0);

    // Edit the product via UI
    await page.goto(`/products/${product.id}/edit`);
    await expect(editPage.heading).toBeVisible();
    await editPage.updateProduct(updatedName, updatedPrice);

    // UI: verify the update on browse page
    await browsePage.goto();
    await expect(browsePage.productByName(updatedName)).toBeVisible();

    // API: verify the edit was persisted
    const afterEditRes = await request.get('/api/products');
    const afterEdit = await afterEditRes.json();
    const edited = afterEdit.find((p: { id: number }) => p.id === product.id);
    expect(edited).toBeTruthy();
    expect(edited.name).toBe(updatedName);
    expect(Number(edited.price)).toBeCloseTo(Number(updatedPrice), 0);

    // Delete via API
    const deleteRes = await request.delete(`/api/products/${product.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // UI: verify it's gone from browse
    await browsePage.goto();
    await expect(browsePage.productByName(updatedName)).not.toBeVisible();

    // API: verify it's gone from the list
    const afterDeleteRes = await request.get('/api/products');
    const afterDelete = await afterDeleteRes.json();
    expect(afterDelete.find((p: { id: number }) => p.id === product.id)).toBeFalsy();
  });
});
