import path from 'node:path';
import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { OrdersCreatePage } from '../../pages/orders/create.page';
import { OrdersListPage } from '../../pages/orders/list.page';
import { ProductsCreatePage } from '../../pages/products/create.page';

const authFile = path.resolve(__dirname, '../../auth/.auth/user.json');

test.describe('Orders CRUD', () => {
  test.describe.configure({ mode: 'serial' });

  let adminUserId: string;
  let productName: string;
  let orderCountBefore: number;

  test.beforeAll(async ({ browser }) => {
    const context = await browser.newContext({ storageState: authFile });
    const page = await context.newPage();
    const response = await page.request.get('/api/users');
    const users = await response.json();
    adminUserId = users[0].id;

    // Track order count before tests
    const ordersRes = await page.request.get('/api/orders');
    const orders = await ordersRes.json();
    orderCountBefore = orders.length;

    await context.close();
  });

  test.afterAll(async ({ request }) => {
    if (productName) {
      const productsRes = await request.get('/api/products');
      if (productsRes.ok()) {
        const products = await productsRes.json();
        const product = products.find((p: { name: string }) => p.name === productName);
        if (product) {
          await request.delete(`/api/products/${product.id}`).catch(() => {});
        }
      }
    }
  });

  test('create an order and verify it appears in the list', async ({ page, request }) => {
    productName = faker.commerce.productName();
    const quantity = String(faker.number.int({ min: 1, max: 5 }));

    // Create a product first via UI
    const productsPage = new ProductsCreatePage(page);
    await productsPage.goto();
    await productsPage.createProduct(productName, faker.commerce.price({ min: 5, max: 100 }));

    // API: verify the product was created
    const productsRes = await request.get('/api/products');
    const products = await productsRes.json();
    expect(products.find((p: { name: string }) => p.name === productName)).toBeTruthy();

    // Create an order via UI
    const createPage = new OrdersCreatePage(page);
    const listPage = new OrdersListPage(page);

    await createPage.goto();
    await expect(createPage.heading).toBeVisible();
    await createPage.createOrder(adminUserId, 0, quantity);

    // UI: verify the order appears in the list
    await listPage.goto();
    await expect(listPage.heading).toBeVisible();
    await expect(listPage.orderRowByUser(adminUserId).first()).toBeVisible();

    // API: verify the order count increased
    const ordersRes = await request.get('/api/orders');
    expect(ordersRes.ok()).toBeTruthy();
    const orders = await ordersRes.json();
    expect(orders.length).toBeGreaterThan(orderCountBefore);
  });

  test('delete an order from the list', async ({ page, request }) => {
    const listPage = new OrdersListPage(page);

    // UI: navigate and delete
    await listPage.goto();
    await expect(listPage.orderRowByUser(adminUserId).first()).toBeVisible();

    page.on('dialog', (dialog) => dialog.accept());
    await listPage.deleteButton(adminUserId).first().click();
    await page.waitForLoadState('networkidle');

    // API: verify the orders endpoint is accessible after delete
    const ordersRes = await request.get('/api/orders');
    expect(ordersRes.ok()).toBeTruthy();
  });
});
