import { expect, test } from '../../fixtures/base';
import { OrdersCreatePage } from '../../pages/orders/create.page';
import { OrdersEditPage } from '../../pages/orders/edit.page';
import { OrdersListPage } from '../../pages/orders/list.page';
import { ProductsCreatePage } from '../../pages/products/create.page';

test.describe('Orders CRUD', () => {
  // Run serially — tests share state via the orders list
  test.describe.configure({ mode: 'serial' });

  let adminUserId: string;

  test.beforeAll(async ({ browser }) => {
    // Get the admin user's actual ID via API
    const context = await browser.newContext({
      storageState: 'auth/.auth/user.json',
    });
    const page = await context.newPage();
    const response = await page.request.get('https://localhost:5001/api/users');
    const users = await response.json();
    adminUserId = users[0].id;
    await context.close();
  });

  test('create an order and verify it appears in the list', async ({ page }) => {
    // Create a product first so the order form's dropdown is populated
    const productsPage = new ProductsCreatePage(page);
    await productsPage.goto();
    await productsPage.createProduct(`E2E Order Product ${Date.now()}`, '25.00');

    const createPage = new OrdersCreatePage(page);
    const listPage = new OrdersListPage(page);

    // Navigate to create page
    await createPage.goto();
    await expect(createPage.heading).toBeVisible();

    // Create order with real admin user ID
    await createPage.createOrder(adminUserId, 0, '2');

    // Verify an order with admin user ID appears in the list
    await listPage.goto();
    await expect(listPage.heading).toBeVisible();
    await expect(listPage.orderRowByUser(adminUserId).first()).toBeVisible();
  });

  test('delete an order from the list', async ({ page }) => {
    const listPage = new OrdersListPage(page);

    // Navigate to orders list
    await listPage.goto();
    await expect(listPage.orderRowByUser(adminUserId).first()).toBeVisible();

    // Delete the order (confirm dialog will appear)
    page.on('dialog', (dialog) => dialog.accept());
    await listPage.deleteButton(adminUserId).first().click();
    await page.waitForLoadState('networkidle');
  });
});
