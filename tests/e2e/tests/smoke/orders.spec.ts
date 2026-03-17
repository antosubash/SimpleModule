import { expect, test } from '../../fixtures/base';
import { OrdersCreatePage } from '../../pages/orders/create.page';
import { OrdersListPage } from '../../pages/orders/list.page';

test.describe('Orders pages', () => {
  test('list page loads', async ({ page }) => {
    const list = new OrdersListPage(page);
    await list.goto();
    await expect(list.heading).toBeVisible();
  });

  test('create page loads', async ({ page }) => {
    const create = new OrdersCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });
});
